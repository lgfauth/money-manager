using FluentValidation;
using MoneyManager.Application.DTOs.Request;

namespace MoneyManager.Application.Validators;

public class CreateSubscriptionValidator : AbstractValidator<CreateSubscriptionRequestDto>
{
    public CreateSubscriptionValidator()
    {
        RuleFor(x => x.PayerName).NotEmpty().WithMessage("Nome é obrigatório");
        RuleFor(x => x.PayerCpf).NotEmpty().Length(11).WithMessage("CPF inválido");
        RuleFor(x => x.PayerEmail).NotEmpty().EmailAddress().WithMessage("E-mail inválido");
    }
}
