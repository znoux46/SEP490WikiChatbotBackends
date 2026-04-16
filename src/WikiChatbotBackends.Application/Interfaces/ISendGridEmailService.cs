namespace WikiChatbotBackends.Application.Interfaces;

public interface ISendGridEmailService
{
    Task SendOtpEmailAsync(string email, string otp, string fullName);
    Task SendPasswordResetConfirmationAsync(string email, string fullName);
}
