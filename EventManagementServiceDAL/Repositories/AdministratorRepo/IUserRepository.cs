using EventManagementServiceDAL.Models;

namespace EventManagementServiceDAL.Repositories.AdministratorRepo
{
   public interface IUserRepository
   {
       Task<User?> GetUserByIdAsync(long userId);
       Task<User?> GetUserByEmailAsync(string email);
       Task<User?> GetUserByUserNameAsync(string userName);
       Task<List<User>> GetAllUsersAsync(int pageNumber, int pageSize);
       Task<int> GetTotalUserCountAsync();
       Task<List<User>> GetUsersByRoleIdAsync(int roleId);
       Task<User> AddUserAsync(User user);
       Task<User> UpdateUserAsync(User user);
       Task<int> SaveChangesAsync();
   }
}
