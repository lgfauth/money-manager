using FluentValidation;
using MoneyManager.Application.DTOs.Request;

namespace MoneyManager.Application.Validators;

public class CreateCategoryValidator : AbstractValidator<CreateCategoryRequestDto>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required")
            .MinimumLength(3).WithMessage("Category name must have at least 3 characters");

        RuleFor(x => x.Type)
            .GreaterThanOrEqualTo(0).WithMessage("Invalid category type");
    }
}
