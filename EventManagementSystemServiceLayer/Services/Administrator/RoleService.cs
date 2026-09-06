using EventManagementServiceDAL.Repositories.AdministratorRepo;
using EventManagementSystemServiceLayer.DTOs.EventManager;

namespace EventManagementSystemServiceLayer.Services.Administrator
{
   public interface IRoleService
   {
       Task<List<RoleResponseDto>> GetActiveRolesAsync();
   }

   public sealed class RoleService : IRoleService
   {
       private readonly IRoleRepository _roleRepository;

       public RoleService(IRoleRepository roleRepository)
       {
           _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
       }

       public async Task<List<RoleResponseDto>> GetActiveRolesAsync()
       {
           var roles = await _roleRepository.GetActiveRolesAsync();
           return roles.Select(r => new RoleResponseDto
           {
               RoleId = r.RoleId,
               RoleName = r.RoleName,
               IsActive = r.IsActive,
               CreatedAtUtc = r.CreatedAtUtc
           }).ToList();
       }
   }
}
