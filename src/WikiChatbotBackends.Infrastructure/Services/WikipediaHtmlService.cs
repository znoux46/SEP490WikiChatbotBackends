using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using WikiChatbotBackends.Application.DTOs;
using WikiChatbotBackends.Application.Interfaces;

namespace WikiChatbotBackends.Infrastructure.Services;

/// <summary>
/// Service to fetch and process HTML content from Wikipedia
/// </summary>
public class WikipediaHtmlService : IWikipediaHtmlService
{
    private readonly ILogger<WikipediaHtmlService> _logger;

    public WikipediaHtmlService(ILogger<WikipediaHtmlService> logger)
    {
        _logger = logger;
    }

    public async Task<WikipediaHtmlResultDto> FetchProcessedWikipediaHtmlAsync(string pageTitle, string language = "vi")
    {
        // Trim whitespace and invisible characters
        pageTitle = pageTitle?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(pageTitle))
            throw new ArgumentException("Page title cannot be empty", nameof(pageTitle));

        // Handle case where full URL is passed instead of just title
        pageTitle = ExtractPageTitleFromUrl(pageTitle, language);

        if (string.IsNullOrWhiteSpace(pageTitle))
            throw new ArgumentException("Could not extract valid page title from input", nameof(pageTitle));

        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("WikiChatbotBackends/1.0 (https://github.com; contact@example.com)");

        try
        {
            // Format title for API
            var formattedTitle = pageTitle.Replace(" ", "_");

            _logger.LogInformation("Fetching Wikipedia HTML for page: {Title} (language: {Language})", pageTitle, language);

            var pageInfo = await FetchWikipediaPageInfoAsync(httpClient, pageTitle, language);

            var jsonResponse = await FetchWikipediaParseJsonAsync(httpClient, pageTitle, language);
            var root = JObject.Parse(jsonResponse);
            var parseToken = root["parse"];

            if (parseToken == null)
            {
                throw new InvalidOperationException($"Invalid Wikipedia response format: missing parse object. Response: {jsonResponse}");
            }

            var textContent = parseToken["text"]?["*"]?.ToString();
            if (string.IsNullOrWhiteSpace(textContent))
            {
                throw new InvalidOperationException($"Invalid Wikipedia response format: missing parse.text.* content. Response: {jsonResponse}");
            }

            var parseData = new WikipediaParseData
            {
                Title = parseToken["title"]?.ToString() ?? pageTitle,
                PageId = parseToken["pageid"]?.Value<int>() ?? 0,
                DisplayTitle = parseToken["displaytitle"]?.ToString(),
                Images = parseToken["images"]?.ToObject<List<string>>(),
                Text = new WikipediaTextContent
                {
                    Content = textContent
                }
            };

            // Process HTML
            var cleanedHtml = ProcessWikipediaHtml(parseData, language);
            var canonicalWikipediaUrl = $"https://{language}.wikipedia.org/wiki/{Uri.EscapeDataString(formattedTitle)}";
            var resolvedThumbnailUrl = pageInfo.ThumbnailUrl;

            if (string.IsNullOrWhiteSpace(resolvedThumbnailUrl))
            {
                resolvedThumbnailUrl = ExtractFirstImageUrlFromHtml(cleanedHtml, language);
            }

            return new WikipediaHtmlResultDto
            {
                HtmlContent = cleanedHtml,
                WikipediaUrl = canonicalWikipediaUrl,
                ThumbnailUrl = resolvedThumbnailUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching and processing Wikipedia HTML: {Title}", pageTitle);
            throw;
        }
    }

    /// <summary>
    /// Extract page title from URL or return as-is if it's already a title
    /// Handles formats like: https://vi.wikipedia.org/?curid=42184 or https://vi.wikipedia.org/wiki/Page_Title
    /// </summary>
    private string ExtractPageTitleFromUrl(string input, string language)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        // Check if input is a URL
        if (input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            input.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var uri = new Uri(input);

                // Case 1: Extract from path like /wiki/Page_Title
                if (uri.AbsolutePath.Contains("/wiki/"))
                {
                    var parts = uri.AbsolutePath.Split(new[] { "/wiki/" }, StringSplitOptions.None);
                    if (parts.Length > 1)
                    {
                        var title = Uri.UnescapeDataString(parts[1]).Replace("_", " ");
                        if (!string.IsNullOrWhiteSpace(title))
                            return title;
                    }
                }

                // Case 2: Extract curid from query string: ?curid=42184
                if (!string.IsNullOrWhiteSpace(uri.Query))
                {
                    var query = uri.Query.TrimStart('?');
                    var parameters = query.Split('&');
                    foreach (var param in parameters)
                    {
                        if (param.StartsWith("curid=", StringComparison.OrdinalIgnoreCase))
                        {
                            var curId = param.Substring(6);
                            if (!string.IsNullOrWhiteSpace(curId))
                            {
                                // Use the MediaWiki API to get the page title from curid
                                return FetchPageTitleFromCuridAsync(curId, language).GetAwaiter().GetResult();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract page title from URL: {Url}", input);
            }

            // If we can't parse it as URL, return original input
            return input;
        }

        // Not a URL, return as-is
        return input;
    }

    /// <summary>
    /// Get page title from Wikipedia curid (page ID)
    /// </summary>
    private async Task<string> FetchPageTitleFromCuridAsync(string curid, string language)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var apiUrl = $"https://{language}.wikipedia.org/w/api.php?action=query&pageids={curid}&format=json&prop=info";
            var response = await httpClient.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
                return curid; // Fallback to curid if request fails

            var json = await response.Content.ReadAsStringAsync();
            var root = JObject.Parse(json);
            var pages = root["query"]?["pages"] as JObject;

            if (pages != null)
            {
                foreach (var page in pages.Properties())
                {
                    var title = page.Value?["title"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(title))
                        return title;
                }
            }

            return curid;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch page title from curid: {Curid}", curid);
            return curid;
        }
    }

    private sealed class WikipediaPageInfo
    {
        public string? ThumbnailUrl { get; init; }
    }

    private async Task<WikipediaPageInfo> FetchWikipediaPageInfoAsync(HttpClient httpClient, string pageTitle, string language)
    {
        // Trim and clean the pageTitle
        pageTitle = pageTitle?.Trim() ?? "";

        // Remove any invisible/zero-width characters
        pageTitle = Regex.Replace(pageTitle, @"[\p{Cf}\p{Cc}]", "").Trim();

        if (string.IsNullOrWhiteSpace(pageTitle))
            throw new ArgumentException("Page title cannot be empty after cleaning", nameof(pageTitle));

        var formattedTitle = pageTitle.Replace(" ", "_");
        var encodedTitle = Uri.EscapeDataString(formattedTitle);

        var apiUrl = $"https://{language}.wikipedia.org/w/api.php?action=query&titles={encodedTitle}&redirects=1&prop=pageimages|info&inprop=url&piprop=thumbnail|original&pithumbsize=500&format=json";
        var response = await httpClient.GetAsync(apiUrl);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to verify Wikipedia page: {response.StatusCode}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var root = JObject.Parse(json);
        var pages = root["query"]?["pages"] as JObject;

        if (pages == null || !pages.Properties().Any())
        {
            throw new InvalidOperationException($"Invalid Wikipedia existence response format. Response: {json}");
        }

        foreach (var page in pages.Properties())
        {
            if (page.Name == "-1" || page.Value?["missing"] != null)
            {
                throw new KeyNotFoundException($"Wikipedia page not found: {pageTitle}");
            }

            var thumbnailUrl =
                page.Value?["thumbnail"]?["source"]?.ToString()
                ?? page.Value?["original"]?["source"]?.ToString();

            if (string.IsNullOrWhiteSpace(thumbnailUrl))
            {
                thumbnailUrl = await FetchSummaryThumbnailAsync(httpClient, formattedTitle, language);
            }

            return new WikipediaPageInfo
            {
                ThumbnailUrl = string.IsNullOrWhiteSpace(thumbnailUrl) ? null : thumbnailUrl
            };
        }

        throw new InvalidOperationException($"Invalid Wikipedia existence response format. Response: {json}");
    }

    private async Task<string?> FetchSummaryThumbnailAsync(HttpClient httpClient, string formattedTitle, string language)
    {
        try
        {
            var encodedTitle = Uri.EscapeDataString(formattedTitle);
            var summaryUrl = $"https://{language}.wikipedia.org/api/rest_v1/page/summary/{encodedTitle}";
            var summaryResponse = await httpClient.GetAsync(summaryUrl);

            if (!summaryResponse.IsSuccessStatusCode)
            {
                return null;
            }

            var summaryJson = await summaryResponse.Content.ReadAsStringAsync();
            var summaryRoot = JObject.Parse(summaryJson);
            return summaryRoot["thumbnail"]?["source"]?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private async Task<string> FetchWikipediaParseJsonAsync(HttpClient httpClient, string pageTitle, string language)
    {
        // Trim and clean the pageTitle
        pageTitle = pageTitle?.Trim() ?? "";
        pageTitle = Regex.Replace(pageTitle, @"[\p{Cf}\p{Cc}]", "").Trim();

        var formattedTitle = pageTitle.Replace(" ", "_");
        var encodedTitle = Uri.EscapeDataString(formattedTitle);

        var apiUrl = $"https://{language}.wikipedia.org/w/api.php?action=parse&page={encodedTitle}&format=json&prop=text|images|displaytitle&disableeditsection=1";
        var response = await httpClient.GetAsync(apiUrl, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to fetch Wikipedia page: {Title} (Status: {Status})", pageTitle, response.StatusCode);
            throw new HttpRequestException($"Failed to fetch Wikipedia page: {response.StatusCode}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return await reader.ReadToEndAsync();
    }

    /// <summary>
    /// Process Wikipedia HTML to remove unwanted elements and clean up
    /// </summary>
    private string ProcessWikipediaHtml(WikipediaParseData parseData, string language)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(parseData.Text?.Content ?? "");

        // Remove unwanted elements
        RemoveUnwantedElements(htmlDoc);

        // Remove sections and their content: Xem thêm, Chú thích, Sách tham khảo, Liên kết ngoài
        RemoveSectionsByHeading(htmlDoc, new[] { "Xem thêm", "Chú thích", "Sách tham khảo", "Liên kết ngoài" });

        // Remove all <a> tags but keep inner content (including images)
        RemoveAllAnchorTags(htmlDoc);

        // Process images - fix relative URLs
        FixImageUrls(htmlDoc, parseData.Images, language);

        // Get cleaned HTML
        var cleanedHtml = htmlDoc.DocumentNode.OuterHtml;

        // Remove extra whitespace characters
        cleanedHtml = System.Text.RegularExpressions.Regex.Replace(cleanedHtml, @"\r\n|\r|\n", "");
        cleanedHtml = System.Text.RegularExpressions.Regex.Replace(cleanedHtml, @"\s+", " ");

        // Add display title if available
        if (!string.IsNullOrEmpty(parseData.DisplayTitle))
        {
            cleanedHtml = $"<h1>{parseData.DisplayTitle}</h1>{cleanedHtml}";
        }

        return cleanedHtml;
    }

    /// <summary>
    /// Remove unwanted HTML elements
    /// </summary>
    private void RemoveUnwantedElements(HtmlDocument htmlDoc)
    {
        // Remove style, script, link, meta tags
        var elementsToRemove = htmlDoc.DocumentNode.SelectNodes("//style | //script | //link | //meta");
        if (elementsToRemove != null)
        {
            foreach (var element in elementsToRemove)
            {
                element.Remove();
            }
        }

        // Remove elements with class hatnote
        var hatnoteElements = htmlDoc.DocumentNode.SelectNodes("//*[@class='hatnote']");
        if (hatnoteElements != null)
        {
            foreach (var element in hatnoteElements)
            {
                element.Remove();
            }
        }

        // Remove elements with class containing hatnote
        var hatnoteContainElements = htmlDoc.DocumentNode.SelectNodes("//*[contains(@class, 'hatnote')]");
        if (hatnoteContainElements != null)
        {
            foreach (var element in hatnoteContainElements)
            {
                element.Remove();
            }
        }
    }

    // Remove sections by heading (e.g. 'Xem thêm', 'Chú thích')
    private void RemoveSectionsByHeading(HtmlDocument htmlDoc, string[] headings)
    {
        foreach (var heading in headings)
        {
            HtmlNode headingNode = null;

            // Try to find heading with different structures
            // 1. h2 with span
            var nodes = htmlDoc.DocumentNode.SelectNodes($"//h2//span[contains(text(), '{heading}')]");
            if (nodes != null && nodes.Count > 0)
            {
                headingNode = nodes[0].ParentNode;
                while (headingNode != null && headingNode.Name != "h2")
                {
                    headingNode = headingNode.ParentNode;
                }
            }

            // 2. h2 directly containing text
            if (headingNode == null)
            {
                var h2Nodes = htmlDoc.DocumentNode.SelectNodes("//h2");
                if (h2Nodes != null)
                {
                    foreach (var h2 in h2Nodes)
                    {
                        if (h2.InnerText.Contains(heading))
                        {
                            headingNode = h2;
                            break;
                        }
                    }
                }
            }

            if (headingNode == null) continue;

            // Xóa toàn bộ từ heading này đến heading tiếp theo cùng cấp hoặc hết bài
            var toRemove = new List<HtmlNode> { headingNode };
            var next = headingNode.NextSibling;
            while (next != null)
            {
                // Nếu gặp h2 tiếp theo, dừng
                if (next.Name == "h2")
                {
                    break;
                }
                toRemove.Add(next);
                next = next.NextSibling;
            }
            foreach (var n in toRemove)
            {
                n.Remove();
            }
        }
    }

    // Remove all <a> tags but keep inner content (including images)
    private void RemoveAllAnchorTags(HtmlDocument htmlDoc)
    {
        var aNodes = htmlDoc.DocumentNode.SelectNodes("//a");
        if (aNodes == null) return;

        foreach (var a in aNodes.ToList())
        {
            var parent = a.ParentNode;
            if (parent == null) continue;

            // Get innerHTML (HTML content inside <a> tag, including img tags)
            var innerHtml = a.InnerHtml;

            if (string.IsNullOrEmpty(innerHtml))
            {
                // If no content, just remove the <a> tag
                a.Remove();
            }
            else
            {
                // Use a wrapper to handle multiple root elements
                var wrapper = HtmlNode.CreateNode($"<div>{innerHtml}</div>");
                if (wrapper?.ChildNodes != null && wrapper.ChildNodes.Count > 0)
                {
                    // Insert each child of wrapper before the <a> tag
                    foreach (var child in wrapper.ChildNodes.ToList())
                    {
                        parent.InsertBefore(child.Clone(), a);
                    }
                    a.Remove();
                }
                else
                {
                    // If wrapper creation fails, use text only
                    parent.ReplaceChild(htmlDoc.CreateTextNode(a.InnerText), a);
                }
            }
        }
    }

    /// <summary>
    /// Fix image URLs in the HTML
    /// </summary>
    private void FixImageUrls(HtmlDocument htmlDoc, List<string>? images, string language)
    {
        var imgNodes = htmlDoc.DocumentNode.SelectNodes("//img");
        if (imgNodes == null) return;

        foreach (var imgNode in imgNodes)
        {
            var src = imgNode.GetAttributeValue("src", "");

            // If src starts with //, add https:
            if (src.StartsWith("//"))
            {
                imgNode.SetAttributeValue("src", "https:" + src);
            }
            // If src is relative, make it absolute
            else if (!src.StartsWith("http://") && !src.StartsWith("https://") && !string.IsNullOrEmpty(src))
            {
                imgNode.SetAttributeValue("src", $"https://{language}.wikipedia.org" + src);
            }
        }
    }

    private static string? ExtractFirstImageUrlFromHtml(string html, string language)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        var firstImage = htmlDoc.DocumentNode.SelectSingleNode("//img[@src]");
        if (firstImage == null)
        {
            return null;
        }

        var src = firstImage.GetAttributeValue("src", string.Empty)?.Trim();
        if (string.IsNullOrWhiteSpace(src))
        {
            return null;
        }

        if (src.StartsWith("//"))
        {
            return "https:" + src;
        }

        if (src.StartsWith("/"))
        {
            return $"https://{language}.wikipedia.org" + src;
        }

        return src;
    }
}
