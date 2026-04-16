using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WikiChatbotBackends.Application.Interfaces;

namespace WikiChatbotBackends.Application.Services;

public class SendGridEmailService : IEmailService
{
    private readonly SendGridClient _client;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(IConfiguration configuration, ILogger<SendGridEmailService> logger)
    {
        var apiKey = configuration["SendGrid:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("SendGrid API Key is not configured");

        _client = new SendGridClient(apiKey);
        _fromEmail = configuration["SendGrid:FromEmail"] ?? "noreply@wikibot.com";
        _fromName = configuration["SendGrid:FromName"] ?? "WikiChatbot";
        _logger = logger;
    }

    public async Task SendOtpEmailAsync(string email, string otp, string fullName)
    {
        try
        {
            var subject = "Your OTP Code for Password Reset";
            var htmlContent = $@"
<html>
<body style='font-family: Arial, sans-serif; color: #333;'>
    <h2>Password Reset Request</h2>
    <p>Hi {fullName},</p>
    <p>Your OTP code is:</p>
    <div style='background: #f0f0f0; padding: 15px; border-radius: 5px; text-align: center; margin: 20px 0;'>
        <h1 style='color: #007bff; letter-spacing: 5px; margin: 0;'>{otp}</h1>
    </div>
    <p><strong>This code expires in 10 minutes.</strong></p>
    <p>If you didn't request this, please ignore this email.</p>
    <hr/>
    <p><small>WikiChatbot Team</small></p>
</body>
</html>";

            var msg = new SendGridMessage()
            {
                From = new EmailAddress(_fromEmail, _fromName),
                Subject = subject,
                HtmlContent = htmlContent
            };
            msg.AddTo(new EmailAddress(email, fullName));

            var response = await _client.SendEmailAsync(msg);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                _logger.LogInformation($"OTP email sent successfully to {email}");
            }
            else
            {
                _logger.LogError($"Failed to send OTP email to {email}. Status: {response.StatusCode}");
                throw new InvalidOperationException("Failed to send OTP email");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Exception sending OTP email to {email}");
            throw;
        }
    }

    public async Task SendPasswordResetConfirmationAsync(string email, string fullName)
    {
        try
        {
            var subject = "Your Password Has Been Reset";
            var htmlContent = $@"
<html>
<body style='font-family: Arial, sans-serif; color: #333;'>
    <h2>Password Reset Confirmation</h2>
    <p>Hi {fullName},</p>
    <p>Your password has been successfully reset.</p>
    <p>If you didn't make this change, please contact support immediately.</p>
    <hr/>
    <p><small>WikiChatbot Team</small></p>
</body>
</html>";

            var msg = new SendGridMessage()
            {
                From = new EmailAddress(_fromEmail, _fromName),
                Subject = subject,
                HtmlContent = htmlContent
            };
            msg.AddTo(new EmailAddress(email, fullName));

            var response = await _client.SendEmailAsync(msg);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                _logger.LogInformation($"Password reset confirmation email sent to {email}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send password reset confirmation email");
            // Don't throw - confirmation email is not critical
        }
    }
}
