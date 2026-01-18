using System.Security.Cryptography;
using System.Text;
using WikiChatbotBackends.Application.DTOs;
using WikiChatbotBackends.Application.Interfaces;
using WikiChatbotBackends.Domain.Entities;

namespace WikiChatbotBackends.Application.Services;

public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IJwtService _jwtService;

    public AuthService(IRepository<User> userRepository, IJwtService jwtService)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
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
        var users = await _userRepository.FindAsync(u => u.Username == dto.Username);
        var user = users.FirstOrDefault();
        
        if (user == null || !VerifyPassword(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid username or password");

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
