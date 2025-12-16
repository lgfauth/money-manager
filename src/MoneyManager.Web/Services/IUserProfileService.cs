using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;

namespace MoneyManager.Web.Services;

public interface IUserProfileService
{
    Task<UserProfileResponseDto> GetProfileAsync();
    Task<UserProfileResponseDto> UpdateProfileAsync(UpdateProfileRequestDto request);
    Task<bool> ChangePasswordAsync(ChangePasswordRequestDto request);
    Task<UserProfileResponseDto> UpdateEmailAsync(UpdateEmailRequestDto request);
}
