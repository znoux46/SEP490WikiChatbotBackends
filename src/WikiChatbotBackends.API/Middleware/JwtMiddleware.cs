using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WikiChatbotBackends.API.Application.Interfaces;

namespace WikiChatbotBackends.API.Middleware;

public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public JwtMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context, IJwtService jwtService)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (token != null)
        {
            var userId = jwtService.ValidateToken(token);
            if (userId != null)
            {
                // Token is valid, but we don't need to attach user here
                // The JWT Bearer middleware will handle the claims
            }
        }

        await _next(context);
    }
}
