using FluentValidation;
using MoneyManager.Application.DTOs.Request;

namespace MoneyManager.Application.Validators;

public class UpdateCreditCardTransactionValidator : AbstractValidator<UpdateCreditCardTransactionRequestDto>
{
    public UpdateCreditCardTransactionValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Descrição é obrigatória");

        RuleFor(x => x.TotalAmount)
            .GreaterThan(0).WithMessage("Valor deve ser maior que zero");

        RuleFor(x => x.PurchaseDate)
            .NotEmpty().WithMessage("Data da compra é obrigatória");
    }
}
