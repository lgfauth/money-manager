using FluentValidation;
using MoneyManager.Application.DTOs.Request;

namespace MoneyManager.Application.Validators;

public class CreateCreditCardTransactionValidator : AbstractValidator<CreateCreditCardTransactionRequestDto>
{
    public CreateCreditCardTransactionValidator()
    {
        RuleFor(x => x.CreditCardId)
            .NotEmpty().WithMessage("Cartão é obrigatório");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Descrição é obrigatória");

        RuleFor(x => x.TotalAmount)
            .GreaterThan(0).WithMessage("Valor deve ser maior que zero");

        RuleFor(x => x.TotalInstallments)
            .InclusiveBetween(1, 18).WithMessage("Número de parcelas deve estar entre 1 e 18");

        RuleFor(x => x.PurchaseDate)
            .NotEmpty().WithMessage("Data da compra é obrigatória");
    }
}
