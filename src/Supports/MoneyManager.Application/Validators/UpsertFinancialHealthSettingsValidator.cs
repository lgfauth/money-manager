using FluentValidation;
using MoneyManager.Application.DTOs.Request;

namespace MoneyManager.Application.Validators;

public class UpsertFinancialHealthSettingsValidator : AbstractValidator<UpsertFinancialHealthSettingsRequestDto>
{
    public UpsertFinancialHealthSettingsValidator()
    {
        RuleFor(x => x.InvestPercent)
            .InclusiveBetween(1, 70).WithMessage("InvestPercent deve estar entre 1 e 70");

        RuleFor(x => x.ReserveMonths)
            .InclusiveBetween(1, 24).WithMessage("ReserveMonths deve estar entre 1 e 24");

        RuleFor(x => x.FireMultiplier)
            .InclusiveBetween(50, 600).WithMessage("FireMultiplier deve estar entre 50 e 600");

        RuleFor(x => x.FixedExpensePercent)
            .InclusiveBetween(10, 90).WithMessage("FixedExpensePercent deve estar entre 10 e 90");

        RuleFor(x => x.InstallmentPercent)
            .InclusiveBetween(5, 60).WithMessage("InstallmentPercent deve estar entre 5 e 60");
    }
}
