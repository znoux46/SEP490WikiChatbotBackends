using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;
using StackExchange.Redis;
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
        var npgsqlBuilder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            // Server has idle session timeout; send periodic keepalive to prevent stale pooled sessions.
            KeepAlive = 30,
            // Fail faster on dead connections and let retry strategy recover.
            Timeout = 15,
            CommandTimeout = 120,
        };

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                npgsqlBuilder.ConnectionString,
                npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: new[] { "57P05" }
                )
            )
        );

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
        services.AddScoped<IChatHistoryRepository, ChatHistoryRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IDetailRepository, DetailRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDetailService, DetailService>(); // Auto DI with constructor

        // Redis
        var redisConnectionString = configuration["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                ConfigurationOptions options;

                if (Uri.TryCreate(redisConnectionString, UriKind.Absolute, out var redisUri)
                    && (redisUri.Scheme.Equals("redis", StringComparison.OrdinalIgnoreCase)
                        || redisUri.Scheme.Equals("rediss", StringComparison.OrdinalIgnoreCase)))
                {
                    options = new ConfigurationOptions();
                    options.EndPoints.Add(redisUri.Host, redisUri.Port);
                    options.Ssl = redisUri.Scheme.Equals("rediss", StringComparison.OrdinalIgnoreCase);

                    if (!string.IsNullOrWhiteSpace(redisUri.UserInfo))
                    {
                        var userInfoParts = redisUri.UserInfo.Split(':', 2);
                        if (userInfoParts.Length == 2)
                        {
                            options.User = Uri.UnescapeDataString(userInfoParts[0]);
                            options.Password = Uri.UnescapeDataString(userInfoParts[1]);
                        }
                        else
                        {
                            options.Password = Uri.UnescapeDataString(redisUri.UserInfo);
                        }
                    }
                }
                else
                {
                    options = ConfigurationOptions.Parse(redisConnectionString);
                }

                options.AbortOnConnectFail = false;
                return ConnectionMultiplexer.Connect(options);
            });
            services.AddScoped<IDocumentQueueService, RedisDocumentQueueService>();
        }

        // JWT Service
        var jwtSecret = configuration["Jwt:SecretKey"] ?? "YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong!";
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "WikiChatbotBackends";
        var jwtAudience = configuration["Jwt:Audience"] ?? "WikiChatbotBackends";
        var jwtExpirationMinutes = int.Parse(configuration["Jwt:ExpirationMinutes"] ?? "60");

        services.AddScoped<IJwtService>(sp => new JwtService(jwtSecret, jwtIssuer, jwtAudience, jwtExpirationMinutes));

        // Application Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IChatHistoryService, ChatHistoryService>();
        services.AddScoped<IQuestionRewriteService, QuestionRewriteService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IDetailService, DetailService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IDocumentStorageService, CloudinaryDocumentStorageService>();

        // OTP and Email Services
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IEmailService, EmailService>(); // Use EmailService with Gmail SMTP

        // RAG Service with HttpClient
        services.AddHttpClient<IRagService, RagService>();

        // Wikipedia Service - using direct HttpClient creation to avoid reuse issues
        services.AddScoped<IWikipediaService, WikipediaService>();

        // Wikipedia HTML Service
        services.AddScoped<IWikipediaHtmlService, WikipediaHtmlService>();

        return services;
    }
}
