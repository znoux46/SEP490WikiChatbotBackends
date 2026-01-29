using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WikiChatbotBackends.Application.Interfaces;
using WikiChatbotBackends.Application.Services;
using WikiChatbotBackends.Infrastructure.Data;
using WikiChatbotBackends.Infrastructure.Repositories;
using WikiChatbotBackends.Infrastructure.Services;

namespace WikiChatbotBackends.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // JWT Service
        var jwtSecret = configuration["Jwt:SecretKey"] ?? "YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong!";
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "WikiChatbotBackends";
        var jwtAudience = configuration["Jwt:Audience"] ?? "WikiChatbotBackends";
        var jwtExpirationMinutes = int.Parse(configuration["Jwt:ExpirationMinutes"] ?? "60");

        services.AddScoped<IJwtService>(sp => new JwtService(jwtSecret, jwtIssuer, jwtAudience, jwtExpirationMinutes));

        // Application Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IChatHistoryService, ChatHistoryService>();

        // RAG Service with HttpClient
        services.AddHttpClient<IRagService, RagService>();

        return services;
    }
}
