namespace EventManagementSystemServiceLayer.DTOs.Administrator
{
   public class UserCreateDto
   {
       public string Email { get; set; } = null!;
       public string UserName { get; set; } = null!;
       public string DisplayName { get; set; } = null!;
       public string? PhoneNumber { get; set; }
       public int RoleId { get; set; }
   }
}
