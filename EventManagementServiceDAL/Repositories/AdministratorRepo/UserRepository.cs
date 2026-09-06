using EventManagementServiceDAL.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagementServiceDAL.Repositories.AdministratorRepo
{
   public class UserRepository : IUserRepository
   {
       private readonly EventManagementDbContext _context;

       public UserRepository(EventManagementDbContext context)
       {
           _context = context ?? throw new ArgumentNullException(nameof(context));
       }

       private IQueryable<User> GetActiveUsersQuery()
       {
           return _context.Users.Where(u => u.IsActive && u.DeactivatedAtUtc == null);
       }

       public async Task<User?> GetUserByIdAsync(long userId)
       {
           try
           {
               return await GetActiveUsersQuery()
                   .Include(u => u.Role)
                   .FirstOrDefaultAsync(u => u.UserId == userId);
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetUserByIdAsync: {ex.Message}");
               return null;
           }
       }

       public async Task<User?> GetUserByEmailAsync(string email)
       {
           try
           {
               return await GetActiveUsersQuery()
                   .Include(u => u.Role)
                   .FirstOrDefaultAsync(u => u.Email == email);
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetUserByEmailAsync: {ex.Message}");
               return null;
           }
       }

       public async Task<User?> GetUserByUserNameAsync(string userName)
       {
           try
           {
               return await GetActiveUsersQuery()
                   .Include(u => u.Role)
                   .FirstOrDefaultAsync(u => u.UserName == userName);
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetUserByUserNameAsync: {ex.Message}");
               return null;
           }
       }

       public async Task<List<User>> GetAllUsersAsync(int pageNumber, int pageSize)
       {
           try
           {
               if (pageNumber < 1) pageNumber = 1;
               if (pageSize < 1) pageSize = 10;

               return await GetActiveUsersQuery()
                   .Include(u => u.Role)
                   .OrderBy(u => u.UserId)
                   .Skip((pageNumber - 1) * pageSize)
                   .Take(pageSize)
                   .ToListAsync();
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetAllUsersAsync: {ex.Message}");
               return new List<User>();
           }
       }

       public async Task<int> GetTotalUserCountAsync()
       {
           try
           {
               return await GetActiveUsersQuery().CountAsync();
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetTotalUserCountAsync: {ex.Message}");
               return 0;
           }
       }

       public async Task<List<User>> GetUsersByRoleIdAsync(int roleId)
       {
           try
           {
               return await GetActiveUsersQuery()
                   .Where(u => u.RoleId == roleId)
                   .Include(u => u.Role)
                   .OrderBy(u => u.UserId)
                   .ToListAsync();
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetUsersByRoleIdAsync: {ex.Message}");
               return new List<User>();
           }
       }

       public async Task<User> AddUserAsync(User user)
       {
           try
           {
               if (user == null)
                   throw new ArgumentNullException(nameof(user));

               _context.Users.Add(user);
               await SaveChangesAsync();
               return user;
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in AddUserAsync: {ex.Message}");
               throw;
           }
       }

       public async Task<User> UpdateUserAsync(User user)
       {
           try
           {
               if (user == null)
                   throw new ArgumentNullException(nameof(user));

               _context.Users.Update(user);
               await SaveChangesAsync();
               return user;
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in UpdateUserAsync: {ex.Message}");
               throw;
           }
       }

       public async Task<int> SaveChangesAsync()
       {
           try
           {
               return await _context.SaveChangesAsync();
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in SaveChangesAsync: {ex.Message}");
               return -99;
           }
       }
   }
}
