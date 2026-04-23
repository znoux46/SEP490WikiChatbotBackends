using WikiChatbotBackends.Application.DTOs;

namespace WikiChatbotBackends.Application.Interfaces;

/// <summary>
/// Interface for fetching and processing HTML content from Wikipedia
/// </summary>
public interface IWikipediaHtmlService
{
    /// <summary>
    /// Fetch and process Wikipedia HTML content fully in-memory
    /// </summary>
    /// <param name="pageTitle">The title of the Wikipedia page</param>
    /// <param name="language">Language code (e.g., 'vi', 'en')</param>
    /// <returns>Processed HTML, canonical Wikipedia URL and thumbnail URL if available</returns>
    Task<WikipediaHtmlResultDto> FetchProcessedWikipediaHtmlAsync(string pageTitle, string language = "vi");
}
