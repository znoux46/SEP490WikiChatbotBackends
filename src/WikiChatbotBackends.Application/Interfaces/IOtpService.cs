namespace WikiChatbotBackends.Application.Interfaces;

public interface IOtpService
{
    Task<string> GenerateOtpAsync(int userId);
    Task<string?> VerifyOtpAsync(int userId, string otp);
    Task<bool> ValidateResetTokenAsync(int userId, string resetToken);
    Task<int?> GetUserIdByResetTokenAsync(string resetToken);
    Task InvalidateTokenAsync(int userId);
}
