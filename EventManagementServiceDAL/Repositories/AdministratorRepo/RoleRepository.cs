using EventManagementServiceDAL.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagementServiceDAL.Repositories.AdministratorRepo
{
   public interface IRoleRepository
   {
       Task<List<Role>> GetActiveRolesAsync();
       Task<Role?> GetRoleByIdAsync(int roleId);
   }

   public sealed class RoleRepository : IRoleRepository
   {
       private readonly EventManagementDbContext _context;

       public RoleRepository(EventManagementDbContext context)
       {
           _context = context ?? throw new ArgumentNullException(nameof(context));
       }

       public async Task<List<Role>> GetActiveRolesAsync()
       {
           try
           {
               return await _context.Roles.AsNoTracking()
                   .Where(r => r.IsActive)
                   .OrderBy(r => r.RoleId)
                   .ToListAsync();
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetActiveRolesAsync: {ex.Message}");
               return new List<Role>();
           }
       }

       public async Task<Role?> GetRoleByIdAsync(int roleId)
       {
           try
           {
               return await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.RoleId == roleId);
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetRoleByIdAsync: {ex.Message}");
               return null;
           }
       }
   }
}
