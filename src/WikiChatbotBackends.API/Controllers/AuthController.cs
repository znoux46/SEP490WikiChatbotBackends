using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WikiChatbotBackends.Application.DTOs;
using WikiChatbotBackends.Application.Interfaces;
using WikiChatbotBackends.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace WikiChatbotBackends.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IRepository<User> _userRepository;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, IRepository<User> userRepository, ILogger<AuthController> logger)
    {
        _authService = authService;
        _userRepository = userRepository;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<LoginResponseDto>> Register([FromBody] RegisterDto dto)
    {
        try
        {
            var result = await _authService.RegisterAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto dto)
    {
        try
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(new { message = "Invalid token" });

            var user = await _authService.UpdateProfileAsync(userId, dto);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ForgotPasswordResponseDto>> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto?.Email))
                return BadRequest(new { message = "Email không được để trống." });

            var otp = await _authService.ForgotPasswordAsync(dto.Email);

            if (otp == null)
            {
                return BadRequest(new { message = "Email không tồn tại trong hệ thống." });
            }

            return Ok(new ForgotPasswordResponseDto
            {
                Success = true,
                Message = "OTP đã được gửi tới email của bạn."
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning($"Validation failed: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in forgot password");
            return BadRequest(new { message = "Có lỗi xảy ra, vui lòng thử lại." });
        }
    }

    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<ActionResult<VerifyOtpResponseDto>> VerifyOtp([FromBody] VerifyOtpDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Otp))
                return BadRequest(new { message = "Email and OTP are required" });

            // Get user by email to get userId
            var users = await _userRepository.FindAsync(u => u.Email == dto.Email);
            var user = users.FirstOrDefault();
            
            if (user == null)
                return Unauthorized(new { message = "Invalid email or OTP" });

            var resetToken = await _authService.VerifyOtpAsync(user.Id, dto.Otp);
            if (string.IsNullOrEmpty(resetToken))
                return Unauthorized(new { message = "Invalid or expired OTP" });

            return Ok(new VerifyOtpResponseDto
            {
                ResetToken = resetToken,
                Message = "OTP verified successfully"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("set-new-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ForgotPasswordResponseDto>> SetNewPassword([FromBody] SetNewPasswordDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.ResetToken) || string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest(new { message = "Reset token and new password are required" });

            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest(new { message = "Passwords do not match" });

            // Get userId from resetToken via AuthService
            var result = await _authService.SetNewPasswordAsync(0, dto.ResetToken, dto.NewPassword, dto.ConfirmPassword);
            
            if (result)
                return Ok(new ForgotPasswordResponseDto
                {
                    Success = true,
                    Message = "Password has been reset successfully"
                });
            else
                return Unauthorized(new { message = "Invalid or expired reset token" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<UserDto>> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        try
        {
            var user = await _authService.ChangePasswordAsync(dto.OldPassword,dto.NewPassword,dto.ConfirmPassword);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
