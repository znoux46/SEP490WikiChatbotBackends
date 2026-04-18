using WikiChatbotBackends.Application.DTOs;

namespace WikiChatbotBackends.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> RegisterAsync(RegisterDto dto);
    Task<LoginResponseDto> LoginAsync(LoginDto dto);
    Task<UserDto> UpdateProfileAsync(int userId, UpdateProfileDto dto);
    Task<UserDto?> GetUserByIdAsync(int userId);
    Task<string> ForgotPasswordAsync(string email);
    Task<string?> VerifyOtpAsync(int userId, string otp);
    Task<bool> SetNewPasswordAsync(int userId, string resetToken, string newPassword, string confirmPassword);
    Task<bool> ChangePasswordAsync(string oldPassword, string newPassword, string confirmPassword);
}
