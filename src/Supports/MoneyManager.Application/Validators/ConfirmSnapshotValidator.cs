using FluentValidation;
using MoneyManager.Application.DTOs.Request;

namespace MoneyManager.Application.Validators;

public class ConfirmSnapshotValidator : AbstractValidator<ConfirmSnapshotRequestDto>
{
    public ConfirmSnapshotValidator()
    {
        RuleFor(x => x.Buckets)
            .NotNull().WithMessage("Buckets não pode ser nulo")
            .NotEmpty().WithMessage("Buckets não pode ser vazio");

        RuleForEach(x => x.Buckets)
            .ChildRules(bucket =>
            {
                bucket.RuleFor(b => b.ConfirmedBalance)
                    .GreaterThanOrEqualTo(0).WithMessage("ConfirmedBalance deve ser maior ou igual a zero");
            });
    }
}
