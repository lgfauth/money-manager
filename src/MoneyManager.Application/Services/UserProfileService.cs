using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Application.Services;

public interface IUserProfileService
{
    Task<UserProfileResponseDto> GetProfileAsync(string userId);
    Task<UserProfileResponseDto> UpdateProfileAsync(string userId, UpdateProfileRequestDto request);
    Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequestDto request);
    Task<UserProfileResponseDto> UpdateEmailAsync(string userId, UpdateEmailRequestDto request);
}

public class UserProfileService : IUserProfileService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserProfileService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserProfileResponseDto> GetProfileAsync(string userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
            throw new KeyNotFoundException("User not found");

        return new UserProfileResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            FullName = user.FullName,
            Phone = user.Phone,
            ProfilePicture = user.ProfilePicture,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<UserProfileResponseDto> UpdateProfileAsync(string userId, UpdateProfileRequestDto request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
            throw new KeyNotFoundException("User not found");

        user.FullName = request.FullName;
        user.Phone = request.Phone;
        user.ProfilePicture = request.ProfilePicture;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);

        return new UserProfileResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            FullName = user.FullName,
            Phone = user.Phone,
            ProfilePicture = user.ProfilePicture,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequestDto request)
    {
        if (request.NewPassword != request.ConfirmPassword)
            throw new InvalidOperationException("New password and confirmation do not match");

        if (request.NewPassword.Length < 6)
            throw new InvalidOperationException("Password must be at least 6 characters long");

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
            throw new KeyNotFoundException("User not found");

        // Verificar senha atual
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect");

        // Hash da nova senha
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);

        return true;
    }

    public async Task<UserProfileResponseDto> UpdateEmailAsync(string userId, UpdateEmailRequestDto request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
            throw new KeyNotFoundException("User not found");

        // Verificar senha
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Password is incorrect");

        // Verificar se email já existe
        var existingUser = await _unitOfWork.Users.GetByEmailAsync(request.NewEmail);
        if (existingUser != null && existingUser.Id != userId)
            throw new InvalidOperationException("Email already in use");

        user.Email = request.NewEmail;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);

        return new UserProfileResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            FullName = user.FullName,
            Phone = user.Phone,
            ProfilePicture = user.ProfilePicture,
            CreatedAt = user.CreatedAt
        };
    }
}
