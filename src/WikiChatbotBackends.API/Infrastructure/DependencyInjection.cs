using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WikiChatbotBackends.API.Application.Interfaces;
using WikiChatbotBackends.API.Application.Services;
using WikiChatbotBackends.API.Infrastructure.Data;
using WikiChatbotBackends.API.Infrastructure.Repositories;

namespace WikiChatbotBackends.API.Infrastructure;

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
        services.AddScoped<IPeopleService, PeopleService>();
        services.AddScoped<IAwardService, AwardService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IChatHistoryService, ChatHistoryService>();

        return services;
    }
}
