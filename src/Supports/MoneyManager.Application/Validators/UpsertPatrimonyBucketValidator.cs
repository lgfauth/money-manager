using FluentValidation;
using MoneyManager.Application.DTOs.Request;

namespace MoneyManager.Application.Validators;

public class UpsertPatrimonyBucketValidator : AbstractValidator<UpsertPatrimonyBucketRequestDto>
{
    public UpsertPatrimonyBucketValidator()
    {
        RuleFor(x => x.Type)
            .Must(t => t == "emergency_reserve" || t == "fire_investment")
            .WithMessage("Type deve ser 'emergency_reserve' ou 'fire_investment'");

        RuleFor(x => x.InitialBalance)
            .GreaterThanOrEqualTo(0).WithMessage("InitialBalance deve ser maior ou igual a zero");

        RuleFor(x => x.InitialBalanceDate)
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("InitialBalanceDate não pode ser futura");

        RuleFor(x => x.ExpectedAnnualRate)
            .InclusiveBetween(0m, 1m).WithMessage("ExpectedAnnualRate deve estar entre 0 e 1 (ex: 0.105 para 10,5%)");

        RuleFor(x => x.TrackedCategoryIds)
            .NotNull().WithMessage("TrackedCategoryIds não pode ser nulo");
    }
}
