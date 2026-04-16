using WikiChatbotBackends.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace WikiChatbotBackends.Application.Services;

public class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;

    public MockEmailService(ILogger<MockEmailService> logger)
    {
        _logger = logger;
    }

    public async Task SendOtpEmailAsync(string email, string otp, string fullName)
    {
        // Log OTP to console instead of sending email
        _logger.LogWarning($"==================== OTP EMAIL ====================");
        _logger.LogWarning($"To: {email}");
        _logger.LogWarning($"Name: {fullName}");
        _logger.LogWarning($"OTP Code: {otp}");
        _logger.LogWarning($"Expires in: 10 minutes");
        _logger.LogWarning($"====================================================");
        await Task.CompletedTask;
    }

    public async Task SendPasswordResetConfirmationAsync(string email, string fullName)
    {
        // Log confirmation to console
        _logger.LogInformation($"✓ Password reset confirmation sent to {email}");
        await Task.CompletedTask;
    }
}
