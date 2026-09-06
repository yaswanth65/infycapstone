using EventManagementSystemServiceLayer.Constants;
using EventManagementSystemServiceLayer.DTOs;
using EventManagementSystemServiceLayer.DTOs.Auth;
using EventManagementSystemServiceLayer.Services.Administrator;
using EventManagementSystemServiceLayer.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagementSystemServiceLayer.Controllers
{
   [ApiController]
   [Route("api/v1/auth")]
   public class AuthController : ControllerBase
   {
       private readonly IAuthService _authService;
       private readonly IAuditLoggingService _auditLoggingService;

       public AuthController(IAuthService authService, IAuditLoggingService auditLoggingService)
       {
           _authService = authService;
           _auditLoggingService = auditLoggingService;
       }

       [HttpPost("login")]
       [AllowAnonymous]
       public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
       {
           if (request == null || string.IsNullOrWhiteSpace(request.EmailOrUserName) || string.IsNullOrWhiteSpace(request.Password))
               return BadRequest(new ApiResponse<object>(false, 400, "Email/username and password are required."));

           var result = await _authService.LoginAsync(request.EmailOrUserName, request.Password);

           var actorUserId = result.UserId ?? 0;
           await LogAuditAsync(actorUserId, result.Success ? AuditActionTypes.Login : AuditActionTypes.LoginFailed,
               "User", actorUserId, result.Success ? "Success" : "Failed", GetClientIpAddress(),
               string.IsNullOrWhiteSpace(request.EmailOrUserName) ? null : $"{{\"identity\":\"{Escape(request.EmailOrUserName)}\"}}");

           if (!result.Success)
               return Unauthorized(new ApiResponse<object>(false, 401, result.FailureReason ?? "Invalid credentials."));

           var response = new LoginResponseDto
           {
               Success = true,
               AccessToken = result.AccessToken,
               ExpiresAtUtc = result.ExpiresAtUtc,
               UserId = result.UserId,
               UserName = result.UserName,
               DisplayName = result.DisplayName,
               Email = result.Email,
               RoleName = result.RoleName,
               Message = "Login successful."
           };

           return Ok(new ApiResponse<LoginResponseDto>(true, 200, "Login successful.", response));
       }

       [HttpPost("signup")]
       [AllowAnonymous]
       public async Task<IActionResult> Signup([FromBody] SignupRequestDto request)
       {
           if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
               return BadRequest(new ApiResponse<object>(false, 400, "Email, username, display name, and password are required."));

           var result = await _authService.SignupAttendeeAsync(request);

           var actorUserId = result.UserId ?? 0;
           await LogAuditAsync(actorUserId, AuditActionTypes.UserCreated,
               "User", actorUserId, result.Success ? "Success" : "Failed", GetClientIpAddress(),
               string.IsNullOrWhiteSpace(request.Email) ? null : $"{{\"email\":\"{Escape(request.Email)}\"}}");

           if (!result.Success)
               return BadRequest(new ApiResponse<object>(false, 400, result.FailureReason ?? "Registration failed."));

           var response = new LoginResponseDto
           {
               Success = true,
               AccessToken = result.AccessToken,
               ExpiresAtUtc = result.ExpiresAtUtc,
               UserId = result.UserId,
               UserName = result.UserName,
               DisplayName = result.DisplayName,
               Email = result.Email,
               RoleName = result.RoleName,
               Message = "Registration successful! Welcome as an Attendee."
           };

           return Ok(new ApiResponse<LoginResponseDto>(true, 200, "Registration successful.", response));
       }

       private long GetCurrentUserId()
       {
           var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                             ?? User.FindFirst("sub")
                             ?? User.FindFirst("UserId");
           if (long.TryParse(userIdClaim?.Value, out var userId)) return userId;
           return 0;
       }

       private string GetClientIpAddress()
       {
           var x = Request.Headers["X-Forwarded-For"].FirstOrDefault();
           if (!string.IsNullOrEmpty(x)) return x.Split(',')[0].Trim();
           return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
       }

       private async Task LogAuditAsync(long actorUserId, string actionType, string targetEntity, long targetEntityId, string outcome, string ipAddress, string? metadata)
       {
           try
           {
               await _auditLoggingService.LogAuditAsync(actorUserId, actionType, targetEntity, targetEntityId, outcome, ipAddress, metadata);
           }
           catch { /* best-effort */ }
       }

       private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
   }
}
