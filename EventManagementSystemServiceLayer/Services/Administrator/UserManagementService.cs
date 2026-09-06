using EventManagementServiceDAL.Models;
using EventManagementServiceDAL.Repositories.AdministratorRepo;
using EventManagementSystemServiceLayer.DTOs.Administrator;

namespace EventManagementSystemServiceLayer.Services.Administrator
{
   public interface IUserManagementService
   {
       Task<UserResponseDto?> CreateUserAsync(UserCreateDto createDto);
       Task<UserResponseDto?> GetUserByIdAsync(long userId);
       Task<List<UserResponseDto>> GetAllUsersAsync(int pageNumber = 1, int pageSize = 10);
       Task<UserResponseDto?> UpdateUserRoleAsync(RoleUpdateDto roleUpdateDto);
       Task<UserResponseDto?> DeactivateUserAsync(long userId);
       Task<int> GetTotalUserCountAsync();
   }

   public class UserManagementService : IUserManagementService
   {
       private readonly IUserRepository _userRepository;
       private const string SeededPassword = "Password@123";

       public UserManagementService(IUserRepository userRepository)
       {
           _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
       }

       public async Task<UserResponseDto?> CreateUserAsync(UserCreateDto createDto)
       {
           try
           {
               if (createDto == null)
                   throw new ArgumentNullException(nameof(createDto));

               var existingEmail = await _userRepository.GetUserByEmailAsync(createDto.Email);
               if (existingEmail != null)
                   throw new InvalidOperationException($"Email '{createDto.Email}' is already in use.");

               var existingUsername = await _userRepository.GetUserByUserNameAsync(createDto.UserName);
               if (existingUsername != null)
                   throw new InvalidOperationException($"Username '{createDto.UserName}' is already in use.");

                var newUser = new User
                {
                    Email = createDto.Email,
                    UserName = createDto.UserName,
                    DisplayName = createDto.DisplayName,
                    PhoneNumber = createDto.PhoneNumber,
                    RoleId = createDto.RoleId,
                    PasswordHash = System.Text.Encoding.UTF8.GetBytes(SeededPassword),
                    PasswordSalt = new byte[16],
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                };

               await _userRepository.AddUserAsync(newUser);

               return MapToResponseDto(newUser);
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in CreateUserAsync: {ex.Message}");
               throw;
           }
       }

       public async Task<UserResponseDto?> GetUserByIdAsync(long userId)
       {
           try
           {
               var user = await _userRepository.GetUserByIdAsync(userId);
               return user != null ? MapToResponseDto(user) : null;
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetUserByIdAsync: {ex.Message}");
               return null;
           }
       }

       public async Task<List<UserResponseDto>> GetAllUsersAsync(int pageNumber = 1, int pageSize = 10)
       {
           try
           {
               var users = await _userRepository.GetAllUsersAsync(pageNumber, pageSize);
               return users.Select(MapToResponseDto).ToList();
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetAllUsersAsync: {ex.Message}");
               return new List<UserResponseDto>();
           }
       }

       public async Task<UserResponseDto?> UpdateUserRoleAsync(RoleUpdateDto roleUpdateDto)
       {
           try
           {
               if (roleUpdateDto == null)
                   throw new ArgumentNullException(nameof(roleUpdateDto));

               var user = await _userRepository.GetUserByIdAsync(roleUpdateDto.UserId);
               if (user == null)
                   throw new InvalidOperationException($"User with ID {roleUpdateDto.UserId} not found or is inactive.");

               user.RoleId = roleUpdateDto.RoleId;
               user.UpdatedAtUtc = DateTime.UtcNow;

               await _userRepository.UpdateUserAsync(user);

               return MapToResponseDto(user);
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in UpdateUserRoleAsync: {ex.Message}");
               throw;
           }
       }

       public async Task<UserResponseDto?> DeactivateUserAsync(long userId)
       {
           try
           {
               var user = await _userRepository.GetUserByIdAsync(userId);
               if (user == null)
                   throw new InvalidOperationException($"User with ID {userId} not found or is already inactive.");

               user.IsActive = false;
               user.DeactivatedAtUtc = DateTime.UtcNow;
               user.UpdatedAtUtc = DateTime.UtcNow;

               await _userRepository.UpdateUserAsync(user);

               return MapToResponseDto(user);
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in DeactivateUserAsync: {ex.Message}");
               throw;
           }
       }

       public async Task<int> GetTotalUserCountAsync()
       {
           try
           {
               return await _userRepository.GetTotalUserCountAsync();
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetTotalUserCountAsync: {ex.Message}");
               return 0;
           }
       }

       private UserResponseDto MapToResponseDto(User user)
       {
           return new UserResponseDto
           {
               UserId = user.UserId,
               Email = user.Email,
               UserName = user.UserName,
               DisplayName = user.DisplayName,
               PhoneNumber = user.PhoneNumber,
               RoleId = user.RoleId,
               RoleName = user.Role?.RoleName,
               IsActive = user.IsActive,
               DeactivatedAtUtc = user.DeactivatedAtUtc,
               CreatedAtUtc = user.CreatedAtUtc,
               UpdatedAtUtc = user.UpdatedAtUtc
           };
       }
   }
}
