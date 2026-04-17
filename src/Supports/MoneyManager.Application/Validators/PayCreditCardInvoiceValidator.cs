using FluentValidation;
using MoneyManager.Application.DTOs.Request;

namespace MoneyManager.Application.Validators;

public class PayCreditCardInvoiceValidator : AbstractValidator<PayCreditCardInvoiceRequestDto>
{
    public PayCreditCardInvoiceValidator()
    {
        RuleFor(x => x.PaidWithAccountId)
            .NotEmpty().WithMessage("Conta pagadora é obrigatória");

        RuleFor(x => x.PaidAmount)
            .GreaterThan(0).WithMessage("Valor do pagamento deve ser maior que zero");

        RuleFor(x => x.PaidAt)
            .NotEmpty().WithMessage("Data de pagamento é obrigatória");
    }
}
