using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pathly_Data;
using Pathly_Models;
using Pathly_Utility;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("PathlyConnection"),
        // Azure SQL intermittently drops connections — retry transparently instead of surfacing
        // a 500 to the learner.
        sql => sql.EnableRetryOnFailure());
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    options.User.AllowedUserNameCharacters = "zxcvbnmasdfghjklqwertyuiopZXCVBNMASDFGHJKLQWERTYUIOP0123456789-*/+@!#$%^*()-_=+][}{';:/?.>,<";
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddApplicationDependacy(builder.Configuration);

builder.Services
    .AddAuthentication(options =>
    {
        // AddIdentity() defaults these to its cookie scheme, which would make every
        // [Authorize] endpoint redirect to /Account/Login instead of honouring Bearer
        // tokens — so they must be explicitly pointed at JWT for the API.
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(GetJwtKey(builder.Configuration))),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

// AddIdentity() registers its cookie scheme and can override the JWT defaults set above, which
// makes plain [Authorize] challenge via a 307 redirect to /Account/Login instead of returning a
// JWT 401. Pinning the DEFAULT AUTHORIZATION POLICY to the JWT bearer scheme forces every
// [Authorize] endpoint to authenticate with Bearer tokens regardless of the Identity cookie
// registration — the API is token-only and has no cookie login flow.
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build();
});

static string GetJwtKey(ConfigurationManager configuration)
{
    var key = configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(key))
    {
        throw new InvalidOperationException(
            "The JWT signing key is not configured. Set 'Jwt:Key' via user secrets in development " +
            "or the 'Jwt__Key' environment variable in production.");
    }

    return key;
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        if (origins.Length > 0)
        {
            policy.WithOrigins(origins);
        }

        policy.AllowAnyMethod()
              .AllowAnyHeader()
              // Required so the browser sends/receives the httpOnly refresh-token cookie on
              // cross-origin (frontend → API) auth calls. Safe here because the policy pins an
              // explicit origin allow-list rather than a wildcard.
              .AllowCredentials();
    });
});

builder.Services.AddControllers();

// Throttle the anonymous auth endpoints so scripted signup/login/password-reset floods can't
// hammer the database. Partitioned per client IP with a modest fixed window.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var seederLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await PlanSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(), seederLogger);
    }
    catch (Exception ex)
    {
        seederLogger.LogError(ex, "Failed to seed the plan catalogue.");
    }
}

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;

        if (exception is not null)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(exception, "Unhandled exception on {Method} {Path}.", context.Request.Method, context.Request.Path);
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "server_error",
            message = "An unexpected error occurred. Please try again later."
        });
    });
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowedOrigins");

// Never let a browser or intermediary proxy cache an API response. Auth/session data and
// per-user reports must not be replayable: without this, a shared-device or shared-proxy
// setup could theoretically serve one account's cached response to another.
app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    await next();
});

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>Exposed so integration tests can boot the real application host via
/// WebApplicationFactory&lt;Program&gt;.</summary>
public partial class Program { }
