using FluentValidation;
using MoneyManager.Application.DTOs.Request;

namespace MoneyManager.Application.Validators;

public class ActivatePremiumValidator : AbstractValidator<ActivatePremiumRequestDto>
{
    public ActivatePremiumValidator()
    {
        RuleFor(x => x.DurationDays)
            .GreaterThan(0).WithMessage("Duração deve ser maior que zero")
            .LessThanOrEqualTo(365).WithMessage("Duração máxima é 365 dias");
    }
}
