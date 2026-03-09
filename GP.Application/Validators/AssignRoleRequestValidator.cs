using FluentValidation;
using GP.Application.DTOs.Admin;

namespace GP.Application.Validators
{
    public class AssignRoleRequestValidator : AbstractValidator<AssignRoleRequest>
    {
        public AssignRoleRequestValidator()
        {
            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Role is required.");
        }
    }
}