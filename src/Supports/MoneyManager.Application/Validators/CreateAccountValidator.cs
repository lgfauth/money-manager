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
            .IsInEnum().WithMessage("Invalid account type");

        RuleFor(x => x.InitialBalance)
            .GreaterThanOrEqualTo(0).WithMessage("Initial balance must be greater than or equal to 0");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be a 3-letter ISO code (e.g. BRL, USD, EUR)");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Account color is required")
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Account color must be a valid hex code (e.g. #00C896)");
    }
}
