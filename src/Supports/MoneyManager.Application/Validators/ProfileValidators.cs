using FluentValidation;
using MoneyManager.Application.DTOs.Request;

namespace MoneyManager.Application.Validators;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequestDto>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Senha atual obrigatória");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Nova senha obrigatória")
            .MinimumLength(12).WithMessage("A senha deve ter pelo menos 12 caracteres")
            .Matches("[A-Z]").WithMessage("A senha deve ter pelo menos uma letra maiúscula")
            .Matches("[0-9]").WithMessage("A senha deve ter pelo menos um número")
            .Matches("[^a-zA-Z0-9]").WithMessage("A senha deve ter pelo menos um caractere especial")
            .NotEqual(x => x.CurrentPassword).WithMessage("A nova senha deve ser diferente da senha atual");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirmação da senha obrigatória")
            .Equal(x => x.NewPassword).WithMessage("As senhas não conferem");
    }
}

public class UpdateEmailRequestValidator : AbstractValidator<UpdateEmailRequestDto>
{
    public UpdateEmailRequestValidator()
    {
        RuleFor(x => x.NewEmail)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequestDto>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FullName)
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters")
            .When(x => x.FullName != null);

        RuleFor(x => x.Phone)
            .Matches(@"^\+?[\d\s\-\(\)]{7,20}$").WithMessage("Invalid phone number format")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.PreferredLanguage)
            .Must(lang => lang == null || new[] { "pt-BR", "en-US", "es-ES" }.Contains(lang))
            .WithMessage("Invalid language. Supported: pt-BR, en-US, es-ES");
    }
}

public class DeleteAccountRequestValidator : AbstractValidator<DeleteAccountRequestDto>
{
    public DeleteAccountRequestValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");

        RuleFor(x => x.ConfirmationText)
            .NotEmpty().WithMessage("Confirmation text is required")
            .Equal("DELETE").WithMessage("Confirmation text must be 'DELETE'");
    }
}
