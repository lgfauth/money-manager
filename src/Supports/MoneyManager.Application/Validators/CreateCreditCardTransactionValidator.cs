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
            .Equal(1).When(x => x.IsRefund)
            .WithMessage("Estorno deve ter apenas 1 parcela");

        RuleFor(x => x.TotalInstallments)
            .InclusiveBetween(1, 18).When(x => !x.IsRefund)
            .WithMessage("Número de parcelas deve estar entre 1 e 18");

        // A origem invoice_import e seus campos são exclusivos da confirmação de importação de fatura
        RuleFor(x => x.Source)
            .NotEqual("invoice_import")
            .WithMessage("Origem de transação inválida");

        RuleFor(x => x)
            .Must(x => x.TargetReferenceMonth is null && x.FirstInstallmentNumber is null
                       && x.InvoiceImportId is null && x.ImportFingerprint is null)
            .WithName("Source")
            .WithMessage("Campos de importação de fatura não são aceitos neste cadastro");

        RuleFor(x => x.PurchaseDate)
            .NotEmpty().WithMessage("Data da compra é obrigatória")
            .Must(d => d.Date <= DateTime.UtcNow.Date)
            .WithMessage("A data da compra não pode ser futura")
            .Must(d => d.Date >= DateTime.UtcNow.Date.AddDays(-30))
            .WithMessage("A data da compra não pode ser anterior a 30 dias");
    }
}
