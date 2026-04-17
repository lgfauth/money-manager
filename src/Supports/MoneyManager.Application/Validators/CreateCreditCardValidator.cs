using FluentValidation;
using MoneyManager.Application.DTOs.Request;

namespace MoneyManager.Application.Validators;

public class CreateCreditCardValidator : AbstractValidator<CreateCreditCardRequestDto>
{
    public CreateCreditCardValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome do cartão é obrigatório")
            .MinimumLength(2).WithMessage("Nome deve ter ao menos 2 caracteres");

        RuleFor(x => x.Limit)
            .GreaterThan(0).WithMessage("Limite deve ser maior que zero");

        RuleFor(x => x.ClosingDay)
            .InclusiveBetween(1, 28).WithMessage("Dia de fechamento deve estar entre 1 e 28");

        RuleFor(x => x.BillingDueDay)
            .InclusiveBetween(1, 28).WithMessage("Dia de vencimento deve estar entre 1 e 28");

        RuleFor(x => x.BestPurchaseDay)
            .InclusiveBetween(1, 28)
            .When(x => x.BestPurchaseDay.HasValue)
            .WithMessage("Melhor dia de compra deve estar entre 1 e 28");

        RuleFor(x => x.Color)
            .NotEmpty().Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Cor deve ser um hex válido");

        RuleFor(x => x.Currency)
            .NotEmpty().Length(3);
    }
}
