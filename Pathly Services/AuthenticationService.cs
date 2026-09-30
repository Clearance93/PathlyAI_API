using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Pathly_Core;
using Pathly_Core.Unit;
using Pathly_DTOs;
using Pathly_Helper;
using Pathly_Models;
using Pathly_Interfaces;
using Pathly_Interfaces.IService;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Pathly_Services
{
    public class AuthenticationService : IAuthServiceInterface
    {
        private readonly IUnitOfWork _Unit;
        private readonly IMapper _Mapper;
        private readonly UserManager<ApplicationUser> _UserManager;
        private readonly IConfiguration _Configuration;
        private readonly IEmailSender _EmailSender;
        private readonly IRefreshTokenRepositoryInterface _RefreshTokens;
        private readonly AppSettings _AppSettings;
        private readonly AuthSettings _AuthSettings;

        public AuthenticationService(IUnitOfWork unit,
                                     IMapper mapper,
                                     UserManager<ApplicationUser> userManager,
                                     IConfiguration configuration,
                                     IEmailSender emailSender,
                                     IRefreshTokenRepositoryInterface refreshTokens,
                                     IOptions<AppSettings> appSettings,
                                     IOptions<AuthSettings> authSettings)
        {
            _Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            _Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _EmailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
            _RefreshTokens = refreshTokens ?? throw new ArgumentNullException(nameof(refreshTokens));
            _AppSettings = appSettings?.Value ?? new AppSettings();
            _AuthSettings = authSettings?.Value ?? new AuthSettings();
        }

        public async Task<ResponseUserDto> AddNewUserAsync(UserDto dto)
        {
            if (!dto.AcceptTerms)
            {
                throw new ArgumentException(
                    "You must accept the Terms of Service and Privacy Policy before creating an account.");
            }

            // Reject disposable inboxes so scripted accounts can't be spun up en masse.
            if (DisposableEmailDomains.IsDisposable(dto.Email))
            {
                throw new ArgumentException(
                    "Please register with a permanent email address — disposable email providers are not accepted.");
            }

            var existingUser = await _Unit.User.GetTheUserByEmail(dto.Email!);

            if (existingUser != null)
            {
                throw new KeyNotFoundException($"User with the email: {dto.Email} already exist");
            }

            var user = _Mapper.Map<ApplicationUser>(dto);

            user.Id = Guid.NewGuid().ToString();
            user.CreatedAt = DateTime.UtcNow;
            user.UserName = dto.Email;
            user.PhoneNumber = dto.PhoneNumber;
            user.FullName = dto.FullName;

            // POPIA: record exactly what was consented to and when. Marketing is opt-in only.
            user.TermsAcceptedAtUtc = DateTime.UtcNow;
            user.TermsVersion = _AppSettings.TermsVersion;
            user.MarketingConsent = dto.MarketingConsent;

            var result = await _UserManager.CreateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));

                throw new InvalidOperationException($"User creation failed: {errors}");
            }

            // Carry the new account's id through so the response includes it (the UI links
            // psychometric submissions against this id).
            dto.Id = user.Id;

            // Send the verification email. This never blocks or fails registration — the account
            // exists and can be used immediately unless the deployment requires confirmation.
            await SendConfirmationEmailAsync(user);

            return await IssueTokensAsync(dto);
        }

        public async Task<ResponseUserDto> AuthenticateTheUserAsync(LoginDto dto)
        {
            var existingUser = await _Unit.User.GetTheUserByEmail(dto.Email!);

            if (existingUser == null)
            {
                throw new InvalidCredentialsException("Invalid email or password");
            }

            var user = existingUser;

            if (user.LockoutEnd is not null && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
                throw new AccountLockedException(
                    "Your account is locked due to multiple failed login attempts. Please try again in 15 minutes.",
                    user.LockoutEnd);
            }

            var results = await _UserManager.CheckPasswordAsync(user, dto.Password ?? string.Empty);

            if (!results)
            {
                user.AccessFailedCount++;

                if (user.AccessFailedCount >= 5)
                {
                    user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);

                    user.LockoutEnabled = true;

                    _Unit.User.Update(user);

                    await _Unit.SaveChangesAsync();

                    throw new AccountLockedException(
                        "Your account is locked due to multiple failed login attempts. Please try again in 15 minutes.",
                        user.LockoutEnd);
                }

                _Unit.User.Update(user);

                await _Unit.SaveChangesAsync();
            }
            else
            {
                // Only enforced when the deployment opts in (Auth:RequireConfirmedEmail).
                if (_AuthSettings.RequireConfirmedEmail && !user.EmailConfirmed)
                {
                    throw new EmailNotConfirmedException(
                        "Please confirm your email address before signing in. Check your inbox for the verification link.");
                }

                user.AccessFailedCount = 0;

                _Unit.User.Update(user);

                await _Unit.SaveChangesAsync();

                return await IssueTokensAsync(_Mapper.Map<UserDto>(user));
            }

            throw new InvalidCredentialsException("Invalid email or password");
        }

        public async Task RequestPasswordResetAsync(ForgotPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.Email))
            {
                return;
            }

            var user = await _UserManager.FindByEmailAsync(dto.Email);

            // Always behave identically whether or not the account exists (no account enumeration).
            if (user is null)
            {
                return;
            }

            var token = await _UserManager.GeneratePasswordResetTokenAsync(user);

            var resetUrl =
                $"{_AppSettings.FrontendBaseUrl.TrimEnd('/')}/reset-password" +
                $"?email={Uri.EscapeDataString(user.Email ?? dto.Email)}" +
                $"&token={Uri.EscapeDataString(token)}";

            var body = $@"
                <p>Hello {System.Net.WebUtility.HtmlEncode(user.FullName ?? "there")},</p>
                <p>We received a request to reset your Pathly password. This link expires in 30 minutes.</p>
                <p><a href=""{resetUrl}"">Reset my password</a></p>
                <p>If you didn't request this, you can safely ignore this email — your password will not change.</p>";

            await _EmailSender.SendAsync(user.Email ?? dto.Email, "Reset your Pathly password", body);
        }

        public async Task ResetPasswordAsync(ResetPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.Email) ||
                string.IsNullOrWhiteSpace(dto.Token) ||
                string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                throw new ArgumentException("Email, token and a new password are required.");
            }

            var user = await _UserManager.FindByEmailAsync(dto.Email);

            if (user is null)
            {
                // Do not reveal whether the account exists.
                throw new InvalidOperationException("This password reset link is invalid or has expired.");
            }

            var result = await _UserManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));

                // A bad/expired token and a weak password are both recoverable client errors.
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(errors)
                        ? "This password reset link is invalid or has expired."
                        : errors);
            }
        }

        public async Task ConfirmEmailAsync(ConfirmEmailDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.UserId) || string.IsNullOrWhiteSpace(dto.Token))
            {
                throw new ArgumentException("A user id and token are required to confirm an email address.");
            }

            var user = await _UserManager.FindByIdAsync(dto.UserId);

            if (user is null)
            {
                throw new InvalidOperationException("This verification link is invalid or has expired.");
            }

            var result = await _UserManager.ConfirmEmailAsync(user, dto.Token);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));

                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(errors)
                        ? "This verification link is invalid or has expired."
                        : errors);
            }
        }

        public async Task ResendConfirmationAsync(ForgotPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.Email))
            {
                return;
            }

            var user = await _UserManager.FindByEmailAsync(dto.Email);

            // Silent no-op when the account is unknown or already confirmed (no enumeration).
            if (user is null || user.EmailConfirmed)
            {
                return;
            }

            await SendConfirmationEmailAsync(user);
        }

        private async Task SendConfirmationEmailAsync(ApplicationUser user)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return;
            }

            var token = await _UserManager.GenerateEmailConfirmationTokenAsync(user);

            var verifyUrl =
                $"{_AppSettings.FrontendBaseUrl.TrimEnd('/')}/verify-email" +
                $"?userId={Uri.EscapeDataString(user.Id)}" +
                $"&token={Uri.EscapeDataString(token)}";

            var body = $@"
                <p>Welcome to Pathly, {System.Net.WebUtility.HtmlEncode(user.FullName ?? "there")}!</p>
                <p>Please confirm your email address to secure your account and receive your reports.</p>
                <p><a href=""{verifyUrl}"">Verify my email</a></p>
                <p>If you didn't create this account, you can ignore this email.</p>";

            await _EmailSender.SendAsync(user.Email, "Confirm your Pathly email address", body);
        }

        private async Task<ResponseUserDto> IssueTokensAsync(UserDto dto)
        {
            var response = BuildAccessToken(dto);

            if (!string.IsNullOrWhiteSpace(dto.Id))
            {
                var rawRefreshToken = GenerateRawToken();

                var entity = new RefreshToken
                {
                    RefreshTokenId = Guid.NewGuid(),
                    ApplicationUserId = dto.Id!,
                    TokenHash = HashToken(rawRefreshToken),
                    CreatedAtUtc = DateTime.UtcNow,
                    ExpiresAtUtc = DateTime.UtcNow.AddDays(RefreshTokenDays)
                };

                await _RefreshTokens.AddAsync(entity);
                await _RefreshTokens.SaveChangesAsync();

                response.RefreshToken = rawRefreshToken;
            }

            return response;
        }

        public async Task<ResponseUserDto> RefreshAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new InvalidCredentialsException("A refresh token is required.");
            }

            var stored = await _RefreshTokens.GetByHashAsync(HashToken(refreshToken));

            if (stored is null)
            {
                throw new InvalidCredentialsException("This session is no longer valid. Please sign in again.");
            }

            if (!stored.IsActive)
            {
                // A rotated/revoked token being presented again suggests theft — revoke every
                // active token on the account defensively, then reject.
                var active = await _RefreshTokens.GetActiveForUserAsync(stored.ApplicationUserId);

                foreach (var token in active)
                {
                    token.RevokedAtUtc = DateTime.UtcNow;
                    _RefreshTokens.Update(token);
                }

                if (active.Count > 0)
                {
                    await _RefreshTokens.SaveChangesAsync();
                }

                throw new InvalidCredentialsException("This session is no longer valid. Please sign in again.");
            }

            var user = await _UserManager.FindByIdAsync(stored.ApplicationUserId)
                ?? throw new InvalidCredentialsException("This session is no longer valid. Please sign in again.");

            // Rotate: revoke the presented token, link it to its replacement, and issue a new pair.
            stored.RevokedAtUtc = DateTime.UtcNow;
            var newRawToken = GenerateRawToken();
            stored.ReplacedByTokenHash = HashToken(newRawToken);
            _RefreshTokens.Update(stored);

            var replacement = new RefreshToken
            {
                RefreshTokenId = Guid.NewGuid(),
                ApplicationUserId = stored.ApplicationUserId,
                TokenHash = HashToken(newRawToken),
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(RefreshTokenDays)
            };

            await _RefreshTokens.AddAsync(replacement);
            await _RefreshTokens.SaveChangesAsync();

            var response = BuildAccessToken(_Mapper.Map<UserDto>(user));
            response.RefreshToken = newRawToken;

            return response;
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return;
            }

            var stored = await _RefreshTokens.GetByHashAsync(HashToken(refreshToken));

            if (stored is null || stored.RevokedAtUtc is not null)
            {
                return;
            }

            stored.RevokedAtUtc = DateTime.UtcNow;
            _RefreshTokens.Update(stored);
            await _RefreshTokens.SaveChangesAsync();
        }

        private int RefreshTokenDays => _AuthSettings.RefreshTokenDays > 0 ? _AuthSettings.RefreshTokenDays : 30;

        private int AccessTokenMinutes => _AuthSettings.AccessTokenMinutes > 0 ? _AuthSettings.AccessTokenMinutes : 60;

        private ResponseUserDto BuildAccessToken(UserDto dto)
        {
            var jwtKey = _Configuration["Jwt:Key"];
            var jwtIssuer = _Configuration["Jwt:Issuer"];
            var jwtAudience = _Configuration["Jwt:Audience"];

            if (string.IsNullOrWhiteSpace(jwtKey) ||
                string.IsNullOrWhiteSpace(jwtIssuer) ||
                string.IsNullOrWhiteSpace(jwtAudience))
            {
                throw new InvalidOperationException("JWT configuration is missing.");
            }

            var claims = new List<Claim>
            {
                 new Claim(JwtRegisteredClaimNames.Sub, dto.Email!),
                 new Claim(JwtRegisteredClaimNames.Email, dto.Email!),
                 new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

                 new Claim("extension_userId", dto.Id ?? string.Empty),
                 new Claim("extension_FullName", dto.FullName ?? string.Empty)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                    issuer: jwtIssuer,
                    audience: jwtAudience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(AccessTokenMinutes),
                    signingCredentials: creds
                );

            return new ResponseUserDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpirationDate = token.ValidTo,
                Email = dto.Email,
                UserId = dto.Id,
                FullName = dto.FullName
            };
        }

        private static string GenerateRawToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }

        private static string HashToken(string rawToken)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        }
    }
}
