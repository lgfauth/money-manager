using FluentValidation;
using MoneyManager.Application.DTOs.Request;

namespace MoneyManager.Application.Validators;

/// <summary>
/// Formato da confirmação de importação. As regras de negócio dos itens (data, 60 dias, valor,
/// duplicidade) são revalidadas pelo service contra o cadastro atual.
/// </summary>
public class ConfirmInvoiceImportValidator : AbstractValidator<ConfirmInvoiceImportRequestDto>
{
    public const int MaxInstallments = 48;
    public const int MaxDescriptionLength = 200;

    public ConfirmInvoiceImportValidator()
    {
        RuleFor(x => x.SelectedHolders)
            .NotNull().WithMessage("Informe os titulares escolhidos.");

        RuleFor(x => x.Items)
            .Must(i => i is { Count: > 0 }).WithMessage("Selecione ao menos um lançamento para importar.")
            .Must(i => i is null || i.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() == i.Count)
            .WithMessage("O mesmo lançamento foi enviado mais de uma vez.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Id)
                .NotEmpty().WithMessage("Lançamento sem identificador.");

            item.RuleFor(i => i.Description)
                .MaximumLength(MaxDescriptionLength)
                .WithMessage($"A descrição pode ter no máximo {MaxDescriptionLength} caracteres.");

            item.RuleFor(i => i)
                .Must(i => i.InstallmentNumber is null == i.TotalInstallments is null)
                .WithName("Installments")
                .WithMessage("Informe a parcela atual e o total de parcelas juntos.");

            item.RuleFor(i => i.TotalInstallments)
                .InclusiveBetween(1, MaxInstallments)
                .When(i => i.TotalInstallments is not null)
                .WithMessage($"O total de parcelas deve estar entre 1 e {MaxInstallments}.");

            item.RuleFor(i => i.InstallmentNumber)
                .GreaterThanOrEqualTo(1)
                .LessThanOrEqualTo(i => i.TotalInstallments ?? 1)
                .When(i => i.InstallmentNumber is not null && i.TotalInstallments is not null)
                .WithMessage("A parcela atual deve estar entre 1 e o total de parcelas.");
        });
    }
}
