using FluentValidation;
using MoneyManager.Application.DTOs.Request;

namespace MoneyManager.Application.Validators;

public class CreateTransactionValidator : AbstractValidator<CreateTransactionRequestDto>
{
    public CreateTransactionValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0");

        RuleFor(x => x.Type)
            .GreaterThanOrEqualTo(0).WithMessage("Invalid transaction type");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required");
    }
}
