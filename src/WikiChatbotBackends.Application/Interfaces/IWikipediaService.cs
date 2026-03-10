using WikiChatbotBackends.Application.DTOs;

namespace WikiChatbotBackends.Application.Interfaces;

/// <summary>
/// Interface for Wikipedia service to fetch article content
/// </summary>
public interface IWikipediaService
{
    /// <summary>
    /// Get a summary of a Wikipedia article by name
    /// </summary>
    /// <param name="title">The title of the Wikipedia article</param>
    /// <param name="language">Language code (default: en)</param>
    /// <returns>Wikipedia summary response</returns>
    Task<WikipediaSummaryResponse?> GetArticleSummaryAsync(string title, string language = "en");

    /// <summary>
    /// Search for Wikipedia articles by query
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="language">Language code (default: en)</param>
    /// <param name="limit">Maximum results (default: 10)</param>
    /// <returns>List of search results</returns>
    Task<List<WikipediaSearchResult>> SearchAsync(string query, string language = "en", int limit = 10);
}

