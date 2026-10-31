using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Validators;
using Xunit;

namespace MoneyManager.Tests.Application.Validators;

public class ChangePasswordRequestValidatorTests
{
    private const string ValidPassword = "NovaSenha@2026";

    private readonly ChangePasswordRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidRequest_ShouldPass()
    {
        // Arrange
        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = "SenhaAtual@2026",
            NewPassword = ValidPassword,
            ConfirmPassword = ValidPassword
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithMissingConfirmation_ShouldFailOnConfirmPassword()
    {
        // Arrange
        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = "SenhaAtual@2026",
            NewPassword = ValidPassword
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ChangePasswordRequestDto.ConfirmPassword));
    }

    [Fact]
    public async Task Validate_WithDifferentConfirmation_ShouldFail()
    {
        // Arrange
        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = "SenhaAtual@2026",
            NewPassword = ValidPassword,
            ConfirmPassword = "OutraSenha@2026"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.Contains(result.Errors, e => e.ErrorMessage == "As senhas não conferem");
    }

    [Theory]
    [InlineData("Curta@1")]
    [InlineData("semmaiuscula@2026")]
    [InlineData("SemNumeroAqui@")]
    [InlineData("SemEspecial2026")]
    public async Task Validate_WithWeakPassword_ShouldFailOnNewPassword(string weakPassword)
    {
        // Arrange
        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = "SenhaAtual@2026",
            NewPassword = weakPassword,
            ConfirmPassword = weakPassword
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ChangePasswordRequestDto.NewPassword));
    }

    [Fact]
    public async Task Validate_WithNewPasswordEqualToCurrent_ShouldFail()
    {
        // Arrange
        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = ValidPassword,
            NewPassword = ValidPassword,
            ConfirmPassword = ValidPassword
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.Contains(result.Errors, e => e.ErrorMessage == "A nova senha deve ser diferente da senha atual");
    }
}
