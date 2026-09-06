namespace EventManagementServiceDAL.Models;

public partial class Role
{
   public int RoleId { get; set; }
   public string RoleName { get; set; } = null!;
   public bool IsActive { get; set; }
   public DateTime CreatedAtUtc { get; set; }

   public virtual ICollection<User> Users { get; set; } = new List<User>();
}
