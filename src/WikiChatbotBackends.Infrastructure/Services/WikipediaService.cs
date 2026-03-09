using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using WikiChatbotBackends.Application.DTOs;
using WikiChatbotBackends.Application.Interfaces;

namespace WikiChatbotBackends.Infrastructure.Services;

/// <summary>
/// Implementation of Wikipedia service to fetch article content
/// </summary>
public class WikipediaService : IWikipediaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WikipediaService> _logger;
    private const string WikipediaApiBaseUrl = "https://en.wikipedia.org/api/rest_v1";

    public WikipediaService(HttpClient httpClient, ILogger<WikipediaService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(WikipediaApiBaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        
        // Set User-Agent header as recommended by Wikipedia API
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("WikiChatbotBackends/1.0 (https://github.com; contact@example.com)");
    }

    public async Task<WikipediaSummaryResponse?> GetArticleSummaryAsync(string title)
    {
        try
        {
            _logger.LogInformation("Fetching Wikipedia article: {Title}", title);

            var encodedTitle = Uri.EscapeDataString(title);
            var response = await _httpClient.GetAsync($"/page/summary/{encodedTitle}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Wikipedia article not found: {Title}", title);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<WikipediaSummaryResponse>();
            
            if (result == null)
            {
                _logger.LogWarning("Failed to parse Wikipedia response for: {Title}", title);
                return null;
            }

            _logger.LogInformation("Successfully fetched Wikipedia article: {Title}", result.Title);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching Wikipedia article: {Title}", title);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout while fetching Wikipedia article: {Title}", title);
            throw new Exception("Request to Wikipedia timed out", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Wikipedia article: {Title}", title);
            throw;
        }
    }
}

