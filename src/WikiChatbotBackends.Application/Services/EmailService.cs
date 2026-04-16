using WikiChatbotBackends.Application.Interfaces;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WikiChatbotBackends.Application.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendOtpEmailAsync(string email, string otp, string fullName)
    {
        try
        {
            var smtpHost = _configuration["EmailSettings:SmtpServer"];
            var smtpPort = int.Parse(_configuration["EmailSettings:Port"] ?? "587");
            var smtpUsername = _configuration["EmailSettings:SmtpUser"];
            var smtpPassword = _configuration["EmailSettings:SmtpPass"];
            var fromEmail = _configuration["EmailSettings:FromEmail"];
            var fromName = _configuration["EmailSettings:FromName"] ?? "WikiChatbot";

            using (var client = new SmtpClient(smtpHost, smtpPort))
            {
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                var subject = "Your OTP for Password Reset";
                var body = $@"
                    <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <h2>Password Reset Request</h2>
                            <p>Hi {fullName},</p>
                            <p>We received a request to reset your password. Use the OTP below to proceed:</p>
                            <div style='background-color: #f0f0f0; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                <h3 style='margin: 0; text-align: center; color: #333;'>{otp}</h3>
                            </div>
                            <p><strong>This OTP will expire in 10 minutes.</strong></p>
                            <p>If you did not request a password reset, please ignore this email.</p>
                            <hr style='border: none; border-top: 1px solid #ddd; margin: 20px 0;' />
                            <p style='font-size: 12px; color: #666;'>
                                This is an automated message. Please do not reply to this email.
                            </p>
                        </body>
                    </html>";

                var mailMessage = new MailMessage(new MailAddress(fromEmail, fromName), new MailAddress(email))
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"OTP email sent successfully to {email}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to send OTP email to {email}: {ex.Message}");
            throw new InvalidOperationException("Failed to send OTP email. Please try again later.", ex);
        }
    }

    public async Task SendPasswordResetConfirmationAsync(string email, string fullName)
    {
        try
        {
            var smtpHost = _configuration["EmailSettings:SmtpServer"];
            var smtpPort = int.Parse(_configuration["EmailSettings:Port"] ?? "587");
            var smtpUsername = _configuration["EmailSettings:SmtpUser"];
            var smtpPassword = _configuration["EmailSettings:SmtpPass"];
            var fromEmail = _configuration["EmailSettings:FromEmail"];
            var fromName = _configuration["EmailSettings:FromName"] ?? "WikiChatbot";

            using (var client = new SmtpClient(smtpHost, smtpPort))
            {
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                var subject = "Password Reset Successful";
                var body = $@"
                    <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <h2>Password Reset Successful</h2>
                            <p>Hi {fullName},</p>
                            <p>Your password has been successfully reset.</p>
                            <p>You can now log in with your new password.</p>
                            <p>If you did not make this change, please contact support immediately.</p>
                            <hr style='border: none; border-top: 1px solid #ddd; margin: 20px 0;' />
                            <p style='font-size: 12px; color: #666;'>
                                This is an automated message. Please do not reply to this email.
                            </p>
                        </body>
                    </html>";

                var mailMessage = new MailMessage(new MailAddress(fromEmail, fromName), new MailAddress(email))
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"Password reset confirmation email sent to {email}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to send password reset confirmation email to {email}: {ex.Message}");
            throw new InvalidOperationException("Failed to send confirmation email.", ex);
        }
    }
}
