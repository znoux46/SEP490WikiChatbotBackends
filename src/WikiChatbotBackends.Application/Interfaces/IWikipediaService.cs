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
    /// <returns>Wikipedia summary response</returns>
    Task<WikipediaSummaryResponse?> GetArticleSummaryAsync(string title);
}

