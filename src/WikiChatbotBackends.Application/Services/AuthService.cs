using System.Security.Cryptography;
using System.Text;
using WikiChatbotBackends.Application.DTOs;
using WikiChatbotBackends.Application.Interfaces;
using WikiChatbotBackends.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace WikiChatbotBackends.Application.Services;

public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IOtpService _otpService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IRepository<User> userRepository, 
        IJwtService jwtService,
        IOtpService otpService,
        IEmailService emailService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _otpService = otpService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<LoginResponseDto> RegisterAsync(RegisterDto dto)
    {
        // Check if username already exists
        var existingUser = await _userRepository.FindAsync(u => u.Username == dto.Username);
        if (existingUser.Any())
            throw new InvalidOperationException("Username already exists");

        // Check if email already exists
        var existingEmail = await _userRepository.FindAsync(u => u.Email == dto.Email);
        if (existingEmail.Any())
            throw new InvalidOperationException("Email already exists");

        var passwordHash = HashPassword(dto.Password);
        var user = new User
        {
            Username = dto.Username,
            PasswordHash = passwordHash,
            Email = dto.Email,
            FullName = dto.FullName,
            Role = "User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdUser = await _userRepository.AddAsync(user);
        var token = _jwtService.GenerateToken(createdUser.Id, createdUser.Username, createdUser.Role);

        return new LoginResponseDto
        {
            Token = token,
            User = new UserDto
            {
                Id = createdUser.Id,
                Username = createdUser.Username,
                Email = createdUser.Email,
                FullName = createdUser.FullName,
                AvatarUrl = createdUser.AvatarUrl,
                Role = createdUser.Role
            }
        };
    }

    public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
    {
        var users = await _userRepository.FindAsync(u => u.Username == dto.UsernameOrEmail || u.Email == dto.UsernameOrEmail);
        var user = users.FirstOrDefault();
        
        if (user == null || !VerifyPassword(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid username/email or password");

        var token = _jwtService.GenerateToken(user.Id, user.Username, user.Role);

        return new LoginResponseDto
        {
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                AvatarUrl = user.AvatarUrl,
                Role = user.Role
            }
        };
    }

    public async Task<UserDto> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"User with id {userId} not found");

        user.FullName = dto.FullName;
        user.AvatarUrl = dto.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role
        };
    }

    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return null;

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role
        };
    }

    public async Task<string> ForgotPasswordAsync(string email)
    {
        // Check if user exists
        var users = await _userRepository.FindAsync(u => u.Email == email);
        var user = users.FirstOrDefault();
        
        if (user == null)
            throw new KeyNotFoundException($"No user found with email: {email}");

        try
        {
            // Generate OTP
            var otp = await _otpService.GenerateOtpAsync(user.Id);
            
            // Send OTP email
            await _emailService.SendOtpEmailAsync(email, otp, user.FullName);
            
            _logger.LogInformation($"OTP sent successfully to {email}");
            return otp;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send OTP email to {email}");
            throw new InvalidOperationException("Failed to send OTP. Please try again later.", ex);
        }
    }

    public async Task<string?> VerifyOtpAsync(int userId, string otp)
    {
        // Verify OTP and get reset token
        var resetToken = await _otpService.VerifyOtpAsync(userId, otp);
        return resetToken;
    }

    public async Task<bool> SetNewPasswordAsync(int userId, string resetToken, string newPassword, string confirmPassword)
    {
        // Validate passwords match
        if (newPassword != confirmPassword)
            throw new InvalidOperationException("Passwords do not match");

        // Validate password strength
        if (newPassword.Length < 6)
            throw new InvalidOperationException("Password must be at least 6 characters long");

        // Extract userId from resetToken if userId is 0
        if (userId == 0)
        {
            userId = await _otpService.GetUserIdByResetTokenAsync(resetToken) ?? throw new UnauthorizedAccessException("Invalid or expired reset token");
        }

        // Validate reset token
        var isValidToken = await _otpService.ValidateResetTokenAsync(userId, resetToken);
        if (!isValidToken)
            throw new UnauthorizedAccessException("Invalid or expired reset token");

        // Get user
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"User with id {userId} not found");

        // Update password
        user.PasswordHash = HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        // Invalidate token
        await _otpService.InvalidateTokenAsync(userId);

        // Send confirmation email
        try
        {
            await _emailService.SendPasswordResetConfirmationAsync(user.Email, user.FullName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send password reset confirmation email");
            // Don't throw - password was already reset successfully
        }

        return true;
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private static bool VerifyPassword(string password, string passwordHash)
    {
        var hashOfInput = HashPassword(password);
        return hashOfInput == passwordHash;
    }
}
