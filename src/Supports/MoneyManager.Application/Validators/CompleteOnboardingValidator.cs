using FluentValidation;
using MoneyManager.Application.DTOs.Request;

namespace MoneyManager.Application.Validators;

public class CompleteOnboardingValidator : AbstractValidator<CompleteOnboardingRequestDto>
{
    public CompleteOnboardingValidator()
    {
        RuleFor(x => x.AccountMappings)
            .NotEmpty().WithMessage("Selecione pelo menos uma conta para sincronizar");

        RuleForEach(x => x.AccountMappings).ChildRules(mapping =>
        {
            mapping.RuleFor(m => m.ExternalAccountId)
                .NotEmpty().WithMessage("ID externo da conta é obrigatório");
            mapping.RuleFor(m => m.MoneyManagerAccountId)
                .NotEmpty().WithMessage("Conta do MoneyManager é obrigatória para cada mapeamento");
        });

        RuleFor(x => x.Strategy)
            .IsInEnum().WithMessage("Estratégia inválida");
    }
}
