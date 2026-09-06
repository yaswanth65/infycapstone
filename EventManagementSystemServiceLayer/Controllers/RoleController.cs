using EventManagementSystemServiceLayer.DTOs;
using EventManagementSystemServiceLayer.DTOs.EventManager;
using EventManagementSystemServiceLayer.Services.Administrator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagementSystemServiceLayer.Controllers
{
   [ApiController]
   [Route("api/v1/roles")]
   [Authorize]
   public class RoleController : ControllerBase
   {
       private readonly IRoleService _roleService;

       public RoleController(IRoleService roleService)
       {
           _roleService = roleService;
       }

       [HttpGet]
       public async Task<IActionResult> GetActiveRoles()
       {
           var roles = await _roleService.GetActiveRolesAsync();
           return Ok(new ApiResponse<List<RoleResponseDto>>(true, 200, "Roles retrieved.", roles));
       }
   }
}
