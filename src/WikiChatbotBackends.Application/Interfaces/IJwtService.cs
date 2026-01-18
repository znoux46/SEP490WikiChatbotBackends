namespace WikiChatbotBackends.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(int userId, string username, string role);
    int? ValidateToken(string token);
}
