using WikiChatbotBackends.Application.Interfaces;
using WikiChatbotBackends.Domain.Entities;
using System.Text.RegularExpressions;

namespace WikiChatbotBackends.Application.Services;

public class OtpService : IOtpService
{
    private readonly IRepository<PasswordResetToken> _tokenRepository;
    private const int OTP_LENGTH = 6;
    private const int OTP_EXPIRY_MINUTES = 10;
    private const int RESET_TOKEN_EXPIRY_HOURS = 1;

    public OtpService(IRepository<PasswordResetToken> tokenRepository)
    {
        _tokenRepository = tokenRepository;
    }

    public async Task<string> GenerateOtpAsync(int userId)
    {
        // Generate random 6-digit OTP
        var random = new Random();
        var otp = random.Next(100000, 999999).ToString();

        // Calculate OTP expiry time
        var otpExpiresAt = DateTime.UtcNow.AddMinutes(OTP_EXPIRY_MINUTES);

        // Check if user already has a token record
        var existingTokens = await _tokenRepository.FindAsync(t => t.UserId == userId);
        var existingToken = existingTokens.FirstOrDefault();

        if (existingToken != null)
        {
            // Update existing record
            existingToken.Otp = otp;
            existingToken.OtpExpiresAt = otpExpiresAt;
            await _tokenRepository.UpdateAsync(existingToken);
        }
        else
        {
            // Create new record
            var newToken = new PasswordResetToken
            {
                UserId = userId,
                Otp = otp,
                OtpExpiresAt = otpExpiresAt,
                CreatedAt = DateTime.UtcNow
            };
            await _tokenRepository.AddAsync(newToken);
        }

        return otp;
    }

    public async Task<string?> VerifyOtpAsync(int userId, string otp)
    {
        // Find token record
        var tokens = await _tokenRepository.FindAsync(t => t.UserId == userId);
        var tokenRecord = tokens.FirstOrDefault();

        if (tokenRecord == null || string.IsNullOrEmpty(tokenRecord.Otp))
            return null;

        // Check if OTP matches
        if (tokenRecord.Otp != otp)
            return null;

        // Check if OTP is expired
        if (tokenRecord.OtpExpiresAt.HasValue && DateTime.UtcNow > tokenRecord.OtpExpiresAt)
            return null;

        // Generate reset token
        var resetToken = GenerateResetToken();
        var resetTokenExpiresAt = DateTime.UtcNow.AddHours(RESET_TOKEN_EXPIRY_HOURS);

        // Update record with reset token
        tokenRecord.ResetToken = resetToken;
        tokenRecord.ResetTokenExpiresAt = resetTokenExpiresAt;
        tokenRecord.Otp = null; // Clear OTP after verification
        await _tokenRepository.UpdateAsync(tokenRecord);

        return resetToken;
    }

    public async Task<bool> ValidateResetTokenAsync(int userId, string resetToken)
    {
        // Find token record
        var tokens = await _tokenRepository.FindAsync(t => t.UserId == userId && t.ResetToken == resetToken);
        var tokenRecord = tokens.FirstOrDefault();

        if (tokenRecord == null)
            return false;

        // Check if reset token is expired
        if (tokenRecord.ResetTokenExpiresAt.HasValue && DateTime.UtcNow > tokenRecord.ResetTokenExpiresAt)
            return false;

        return true;
    }

    public async Task InvalidateTokenAsync(int userId)
    {
        // Find and delete token record
        var tokens = await _tokenRepository.FindAsync(t => t.UserId == userId);
        foreach (var token in tokens)
        {
            // Just clear sensitive data instead of delete
            token.Otp = null;
            token.ResetToken = null;
            await _tokenRepository.UpdateAsync(token);
        }
    }

    public async Task<int?> GetUserIdByResetTokenAsync(string resetToken)
    {
        // Find token by resetToken
        var tokens = await _tokenRepository.FindAsync(t => t.ResetToken == resetToken);
        var tokenRecord = tokens.FirstOrDefault();

        if (tokenRecord == null)
            return null;

        // Check if reset token is expired
        if (tokenRecord.ResetTokenExpiresAt.HasValue && DateTime.UtcNow > tokenRecord.ResetTokenExpiresAt)
            return null;

        return tokenRecord.UserId;
    }

    private static string GenerateResetToken()
    {
        var randomBytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
