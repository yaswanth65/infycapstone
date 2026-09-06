using FluentValidation;

namespace EventManagementSystemServiceLayer.Validators.Administrator
{
   public class UserCreateDtoValidator : AbstractValidator<EventManagementSystemServiceLayer.DTOs.Administrator.UserCreateDto>
   {
       public UserCreateDtoValidator()
       {
           RuleFor(x => x.Email)
               .NotEmpty()
               .WithMessage("Email is required.")
               .EmailAddress()
               .WithMessage("Email format is invalid. Please provide a valid email address.");

           RuleFor(x => x.UserName)
               .NotEmpty()
               .WithMessage("Username is required.")
               .MinimumLength(3)
               .WithMessage("Username must be at least 3 characters long.")
               .MaximumLength(100)
               .WithMessage("Username must not exceed 100 characters.");

           RuleFor(x => x.DisplayName)
               .NotEmpty()
               .WithMessage("Display name is required.")
               .MaximumLength(150)
               .WithMessage("Display name must not exceed 150 characters.");

           RuleFor(x => x.PhoneNumber)
               .MaximumLength(20)
               .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
               .WithMessage("Phone number must not exceed 20 characters.");

           RuleFor(x => x.RoleId)
               .NotEmpty()
               .WithMessage("Role ID is required.")
               .GreaterThan(0)
               .WithMessage("Role ID must be a valid positive integer.");
       }
   }
}
