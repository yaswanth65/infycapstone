using System.Text;
using EventManagementServiceDAL.Repositories.AdministratorRepo;
using Microsoft.Extensions.Logging;

namespace EventManagementSystemServiceLayer.Services.Auth
{
   public sealed class LoginResult
   {
       public bool Success { get; init; }
       public string? AccessToken { get; init; }
       public DateTime? ExpiresAtUtc { get; init; }
       public long? UserId { get; init; }
       public string? UserName { get; init; }
       public string? DisplayName { get; init; }
       public string? Email { get; init; }
       public string? RoleName { get; init; }
       public string? FailureReason { get; init; }
   }

    public interface IAuthService
    {
        Task<LoginResult> LoginAsync(string emailOrUserName, string password, CancellationToken cancellationToken = default);
        Task<LoginResult> SignupAttendeeAsync(EventManagementSystemServiceLayer.DTOs.Auth.SignupRequestDto dto, CancellationToken cancellationToken = default);
    }

    public sealed class AuthService : IAuthService
    {
        private readonly IUserRepository _users;
        private readonly IJwtTokenService _tokens;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUserRepository users, IJwtTokenService tokens, ILogger<AuthService> logger)
        {
            _users = users;
            _tokens = tokens;
            _logger = logger;
        }

        public async Task<LoginResult> SignupAttendeeAsync(EventManagementSystemServiceLayer.DTOs.Auth.SignupRequestDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.UserName) || string.IsNullOrWhiteSpace(dto.Password))
                {
                    return new LoginResult { Success = false, FailureReason = "Email, username and password are required." };
                }

                var existingEmail = await _users.GetUserByEmailAsync(dto.Email);
                if (existingEmail != null)
                {
                    return new LoginResult { Success = false, FailureReason = $"Email '{dto.Email}' is already registered." };
                }

                var existingUsername = await _users.GetUserByUserNameAsync(dto.UserName);
                if (existingUsername != null)
                {
                    return new LoginResult { Success = false, FailureReason = $"Username '{dto.UserName}' is already taken." };
                }

                var newUser = new EventManagementServiceDAL.Models.User
                {
                    Email = dto.Email,
                    UserName = dto.UserName,
                    DisplayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? dto.UserName : dto.DisplayName,
                    PhoneNumber = dto.PhoneNumber,
                    RoleId = 1, // Attendee Role ID
                    PasswordHash = Encoding.UTF8.GetBytes(dto.Password),
                    PasswordSalt = new byte[16],
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _users.AddUserAsync(newUser);

                var (jwt, expiresAt) = _tokens.IssueToken(newUser.UserId, newUser.UserName, newUser.Email, "Attendee");

                return new LoginResult
                {
                    Success = true,
                    AccessToken = jwt,
                    ExpiresAtUtc = expiresAt,
                    UserId = newUser.UserId,
                    UserName = newUser.UserName,
                    DisplayName = newUser.DisplayName,
                    Email = newUser.Email,
                    RoleName = "Attendee"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignupAttendeeAsync failed for {Email}", dto.Email);
                return new LoginResult { Success = false, FailureReason = "Registration error." };
            }
        }

       public async Task<LoginResult> LoginAsync(string emailOrUserName, string password, CancellationToken cancellationToken = default)
       {
           try
           {
               if (string.IsNullOrWhiteSpace(emailOrUserName) || string.IsNullOrWhiteSpace(password))
               {
                   return new LoginResult { Success = false, FailureReason = "Email/username and password are required." };
               }

               var user = await _users.GetUserByEmailAsync(emailOrUserName)
                   ?? await _users.GetUserByUserNameAsync(emailOrUserName);

               if (user is null || !user.IsActive || user.DeactivatedAtUtc is not null)
               {
                   return new LoginResult { Success = false, FailureReason = "Invalid credentials." };
               }

// Seed data stores the password as NVARCHAR->VARBINARY (UTF-16LE bytes),
                // while admin-created users store the plain UTF-8 bytes. Accept both so
                // seeded demo accounts and admin-created accounts can all authenticate.
                var hashedBytes = user.PasswordHash ?? Array.Empty<byte>();
                var storedUtf8 = Encoding.UTF8.GetString(hashedBytes);
                var storedUnicode = Encoding.Unicode.GetString(hashedBytes);
                bool verified = string.Equals(storedUtf8, password, StringComparison.Ordinal)
                    || string.Equals(storedUnicode, password, StringComparison.Ordinal);

               if (!verified)
               {
                   return new LoginResult { Success = false, FailureReason = "Invalid credentials." };
               }

               var roleName = user.Role?.RoleName ?? "Attendee";
               var (jwt, expiresAt) = _tokens.IssueToken(user.UserId, user.UserName, user.Email, roleName);

               return new LoginResult
               {
                   Success = true,
                   AccessToken = jwt,
                   ExpiresAtUtc = expiresAt,
                   UserId = user.UserId,
                   UserName = user.UserName,
                   DisplayName = user.DisplayName,
                   Email = user.Email,
                   RoleName = roleName
               };
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "LoginAsync failed for {Identity}", emailOrUserName);
               return new LoginResult { Success = false, FailureReason = "Authentication error." };
           }
       }
   }
}
