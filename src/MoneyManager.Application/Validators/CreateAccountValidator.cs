using FluentValidation;
using MoneyManager.Application.DTOs.Request;

namespace MoneyManager.Application.Validators;

public class CreateAccountValidator : AbstractValidator<CreateAccountRequestDto>
{
    public CreateAccountValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Account name is required")
            .MinimumLength(3).WithMessage("Account name must have at least 3 characters");

        RuleFor(x => x.Type)
            .GreaterThanOrEqualTo(0).WithMessage("Invalid account type");

        RuleFor(x => x.InitialBalance)
            .GreaterThanOrEqualTo(0).WithMessage("Initial balance must be greater than or equal to 0");
    }
}
