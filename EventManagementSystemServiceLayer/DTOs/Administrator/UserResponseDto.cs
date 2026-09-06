namespace EventManagementSystemServiceLayer.DTOs.Administrator
{
   public class UserResponseDto
   {
       public long UserId { get; set; }
       public string Email { get; set; } = null!;
       public string UserName { get; set; } = null!;
       public string DisplayName { get; set; } = null!;
       public string? PhoneNumber { get; set; }
       public int RoleId { get; set; }
       public string? RoleName { get; set; }
       public bool IsActive { get; set; }
       public DateTime? DeactivatedAtUtc { get; set; }
       public DateTime CreatedAtUtc { get; set; }
       public DateTime? UpdatedAtUtc { get; set; }
   }
}
