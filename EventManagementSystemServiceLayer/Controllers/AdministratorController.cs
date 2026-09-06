using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EventManagementSystemServiceLayer.DTOs;
using EventManagementSystemServiceLayer.DTOs.Administrator;
using EventManagementSystemServiceLayer.Services.Administrator;
using EventManagementSystemServiceLayer.Constants;
using FluentValidation;

namespace EventManagementSystemServiceLayer.Controllers
{
   [ApiController]
   [Route("api/v1/admin")]
   [Authorize(Roles = "Administrator")]
   public class AdministratorController : ControllerBase
   {
       private readonly IUserManagementService _userManagementService;
       private readonly IAuditLoggingService _auditLoggingService;
       private readonly IValidator<UserCreateDto> _userCreateValidator;
       private readonly IValidator<RoleUpdateDto> _roleUpdateValidator;

       public AdministratorController(
           IUserManagementService userManagementService,
           IAuditLoggingService auditLoggingService,
           IValidator<UserCreateDto> userCreateValidator,
           IValidator<RoleUpdateDto> roleUpdateValidator)
       {
           _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
           _auditLoggingService = auditLoggingService ?? throw new ArgumentNullException(nameof(auditLoggingService));
           _userCreateValidator = userCreateValidator ?? throw new ArgumentNullException(nameof(userCreateValidator));
           _roleUpdateValidator = roleUpdateValidator ?? throw new ArgumentNullException(nameof(roleUpdateValidator));
       }

       [HttpPost("users")]
       [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status201Created)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
       public async Task<IActionResult> CreateUser([FromBody] UserCreateDto createDto)
       {
           try
           {
               if (createDto == null)
                   return BadRequest(new ApiResponse<object>(false, 400, "Request body cannot be null."));

               var validationResult = await _userCreateValidator.ValidateAsync(createDto);
               if (!validationResult.IsValid)
               {
                   var errors = validationResult.Errors
                       .GroupBy(e => e.PropertyName)
                       .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToList());

                   return BadRequest(new ApiResponse<object>(false, 400, "Validation failed.", null, errors));
               }

               var createdUser = await _userManagementService.CreateUserAsync(createDto);
               if (createdUser == null)
                   return BadRequest(new ApiResponse<object>(false, 400, "Failed to create user. Please check the provided details."));

               await LogAuditAsync(
                   GetCurrentUserId(),
                   AuditActionTypes.UserCreated,
                   "User",
                   createdUser.UserId,
                   "Success",
                   GetClientIpAddress(),
                   $"{{\"Email\":\"{createDto.Email}\",\"RoleId\":{createDto.RoleId}}}");

               return CreatedAtAction(
                   nameof(GetUserById),
                   new { userId = createdUser.UserId },
                   new ApiResponse<UserResponseDto>(true, 201, "User created successfully.", createdUser));
           }
           catch (InvalidOperationException ex)
           {
               return BadRequest(new ApiResponse<object>(false, 400, ex.Message));
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in CreateUser: {ex.Message}");
               return StatusCode(500, new ApiResponse<object>(false, 500, "An error occurred while creating the user."));
           }
       }

       [HttpGet("users/{userId}")]
       [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
       public async Task<IActionResult> GetUserById(long userId)
       {
           try
           {
               var user = await _userManagementService.GetUserByIdAsync(userId);
               if (user == null)
                   return NotFound(new ApiResponse<object>(false, 404, $"User with ID {userId} not found."));

               return Ok(new ApiResponse<UserResponseDto>(true, 200, "User retrieved successfully.", user));
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetUserById: {ex.Message}");
               return StatusCode(500, new ApiResponse<object>(false, 500, "An error occurred while retrieving the user."));
           }
       }

       [HttpGet("users")]
       [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<UserResponseDto>>), StatusCodes.Status200OK)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
       public async Task<IActionResult> GetAllUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
       {
           try
           {
               if (pageNumber < 1) pageNumber = 1;
               if (pageSize < 1) pageSize = 10;
               if (pageSize > 100) pageSize = 100;

               var users = await _userManagementService.GetAllUsersAsync(pageNumber, pageSize);
               var totalCount = await _userManagementService.GetTotalUserCountAsync();

               var paginatedResponse = new PaginatedResponse<UserResponseDto>
               {
                   Data = users,
                   PageNumber = pageNumber,
                   PageSize = pageSize,
                   TotalRecords = totalCount,
                   TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
               };

               return Ok(new ApiResponse<PaginatedResponse<UserResponseDto>>(true, 200, "Users retrieved successfully.", paginatedResponse));
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in GetAllUsers: {ex.Message}");
               return StatusCode(500, new ApiResponse<object>(false, 500, "An error occurred while retrieving users."));
           }
       }

       [HttpPut("users/role")]
       [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
       public async Task<IActionResult> UpdateUserRole([FromBody] RoleUpdateDto roleUpdateDto)
       {
           try
           {
               if (roleUpdateDto == null)
                   return BadRequest(new ApiResponse<object>(false, 400, "Request body cannot be null."));

               var validationResult = await _roleUpdateValidator.ValidateAsync(roleUpdateDto);
               if (!validationResult.IsValid)
               {
                   var errors = validationResult.Errors
                       .GroupBy(e => e.PropertyName)
                       .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToList());

                   return BadRequest(new ApiResponse<object>(false, 400, "Validation failed.", null, errors));
               }

               var updatedUser = await _userManagementService.UpdateUserRoleAsync(roleUpdateDto);
               if (updatedUser == null)
                   return NotFound(new ApiResponse<object>(false, 404, $"User with ID {roleUpdateDto.UserId} not found."));

               await LogAuditAsync(
                   GetCurrentUserId(),
                   AuditActionTypes.RoleAssigned,
                   "User",
                   roleUpdateDto.UserId,
                   "Success",
                   GetClientIpAddress(),
                   $"{{\"NewRoleId\":{roleUpdateDto.RoleId}}}");

               return Ok(new ApiResponse<UserResponseDto>(true, 200, "User role updated successfully.", updatedUser));
           }
           catch (InvalidOperationException ex)
           {
               return BadRequest(new ApiResponse<object>(false, 400, ex.Message));
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in UpdateUserRole: {ex.Message}");
               return StatusCode(500, new ApiResponse<object>(false, 500, "An error occurred while updating the user role."));
           }
       }

       [HttpPost("users/{userId}/deactivate")]
       [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
       [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
       public async Task<IActionResult> DeactivateUser(long userId)
       {
           try
           {
               if (userId <= 0)
                   return BadRequest(new ApiResponse<object>(false, 400, "User ID must be a positive integer."));

               var deactivatedUser = await _userManagementService.DeactivateUserAsync(userId);
               if (deactivatedUser == null)
                   return NotFound(new ApiResponse<object>(false, 404, $"User with ID {userId} not found."));

               await LogAuditAsync(
                   GetCurrentUserId(),
                   AuditActionTypes.UserDeactivated,
                   "User",
                   userId,
                   "Success",
                   GetClientIpAddress(),
                   null);

               return Ok(new ApiResponse<UserResponseDto>(true, 200, "User deactivated successfully.", deactivatedUser));
           }
           catch (InvalidOperationException ex)
           {
               return BadRequest(new ApiResponse<object>(false, 400, ex.Message));
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error in DeactivateUser: {ex.Message}");
               return StatusCode(500, new ApiResponse<object>(false, 500, "An error occurred while deactivating the user."));
           }
       }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? User.FindFirstValue("sub")
                              ?? User.FindFirstValue("UserId");
            if (long.TryParse(userIdClaim, out var userId))
                return userId;
            return 0;
        }

       private string GetClientIpAddress()
       {
           var xForwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
           if (!string.IsNullOrEmpty(xForwardedFor))
               return xForwardedFor.Split(',')[0].Trim();

           return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
       }

       private async Task LogAuditAsync(
           long actorUserId,
           string actionType,
           string targetEntity,
           long targetEntityId,
           string outcome,
           string ipAddress,
           string? metadataJson)
       {
           try
           {
               await _auditLoggingService.LogAuditAsync(
                   actorUserId,
                   actionType,
                   targetEntity,
                   targetEntityId,
                   outcome,
                   ipAddress,
                   metadataJson);
           }
           catch (Exception ex)
           {
               System.Diagnostics.Debug.WriteLine($"Error logging audit trail: {ex.Message}");
           }
       }
   }
}
