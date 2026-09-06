using FluentValidation;

namespace EventManagementSystemServiceLayer.Validators.Administrator
{
   public class RoleUpdateDtoValidator : AbstractValidator<EventManagementSystemServiceLayer.DTOs.Administrator.RoleUpdateDto>
   {
       public RoleUpdateDtoValidator()
       {
           RuleFor(x => x.UserId)
               .NotEmpty()
               .WithMessage("User ID is required.")
               .GreaterThan(0)
               .WithMessage("User ID must be a valid positive integer.");

           RuleFor(x => x.RoleId)
               .NotEmpty()
               .WithMessage("Role ID is required.")
               .GreaterThan(0)
               .WithMessage("Role ID must be a valid positive integer.");
       }
   }
}
