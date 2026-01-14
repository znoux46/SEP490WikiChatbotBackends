using WikiChatbotBackends.API.Application.DTOs;

namespace WikiChatbotBackends.API.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> RegisterAsync(RegisterDto dto);
    Task<LoginResponseDto> LoginAsync(LoginDto dto);
    Task<UserDto> UpdateProfileAsync(int userId, UpdateProfileDto dto);
    Task<UserDto?> GetUserByIdAsync(int userId);
}
