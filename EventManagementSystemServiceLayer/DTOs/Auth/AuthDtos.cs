namespace EventManagementSystemServiceLayer.DTOs.Auth
{
   public class LoginRequestDto
   {
       public string EmailOrUserName { get; set; } = null!;
       public string Password { get; set; } = null!;
   }

   public class LoginResponseDto
   {
       public bool Success { get; set; }
       public string? AccessToken { get; set; }
       public DateTime? ExpiresAtUtc { get; set; }
       public long? UserId { get; set; }
       public string? UserName { get; set; }
       public string? DisplayName { get; set; }
       public string? Email { get; set; }
       public string? RoleName { get; set; }
       public string? Message { get; set; }
   }

   public class SignupRequestDto
   {
       public string Email { get; set; } = null!;
       public string UserName { get; set; } = null!;
       public string DisplayName { get; set; } = null!;
       public string? PhoneNumber { get; set; }
       public string Password { get; set; } = null!;
   }
}
