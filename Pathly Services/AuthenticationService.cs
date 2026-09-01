using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Pathly_Core.Unit;
using Pathly_DTOs;
using Pathly_Helper;
using Pathly_Models;
using PathlyInterfaces.IService;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Pathly_Services
{
    public class AuthenticationService : IAuthServiceInterface
    {
        private readonly IUnitOfWork _Unit;
        private readonly IMapper _Mapper;
        private readonly UserManager<ApplicationUser> _UserManager;
        private readonly IConfiguration _Configuration;

        public AuthenticationService(IUnitOfWork unit,
                                     IMapper mapper,
                                     UserManager<ApplicationUser> userManager,
                                     IConfiguration configuration)
        {
            _Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            _Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<ResponseUserDto> AddNewUserAsync(UserDto dto)
        {
            var existingUser = await _Unit.User.GetTheUserByEmail(dto.Email!);

            if (existingUser != null)
            {
                throw new KeyNotFoundException($"User with the email: {dto.Email} already exist");
            }

            var passwordHasher = new PasswordHasher<ApplicationUser>();

            var user = _Mapper.Map<ApplicationUser>(dto);

            user.Password = passwordHasher.HashPassword(user, dto.Password!);

            user.Id = Guid.NewGuid().ToString();
            user.CreatedAt = DateTime.UtcNow;
            user.UserName = dto.Email;
            user.PhoneNumber = dto.PhoneNumber;

            var result = await _UserManager.CreateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));

                throw new InvalidOperationException($"User creation failed: {errors}");
            }

            // Carry the new account's id through so the response includes it (the UI links
            // psychometric submissions against this id).
            dto.Id = user.Id;

            return await GenerateTokenAsync(dto);
        }

        private async Task<ResponseUserDto> GenerateTokenAsync(UserDto dto)
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
                    expires: DateTime.UtcNow.AddHours(24),
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

            var passwordhasher = new PasswordHasher<ApplicationUser>();

            var hashPassword = user.Password;

            var results = passwordhasher.VerifyHashedPassword(user, hashPassword!, dto.Password ?? string.Empty);

            if (results == PasswordVerificationResult.Failed)
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
                user.AccessFailedCount = 0;

                 _Unit.User.Update(user);

                await _Unit.SaveChangesAsync();

                return await GenerateTokenAsync(_Mapper.Map<UserDto>(user));
            }

            throw new InvalidCredentialsException("Invalid email or password");
        }
    }
}
