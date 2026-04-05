
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using WikiChatbotBackends.Application.DTOs;
using WikiChatbotBackends.Application.Interfaces;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Linq;

namespace WikiChatbotBackends.Infrastructure.Services;

/// <summary>
/// Implementation of Wikipedia service to fetch article content
/// </summary>
public class WikipediaService : IWikipediaService
{
    private readonly ILogger<WikipediaService> _logger;

    public WikipediaService(ILogger<WikipediaService> logger)
    {
        _logger = logger;
    }

    private HttpClient CreateHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            ConnectCallback = async (context, cancellationToken) =>
            {
                // Tìm địa chỉ IP của Wikipedia
                var entry = await Dns.GetHostEntryAsync(context.DnsEndPoint.Host, cancellationToken);
                
                // Ép chọn IPv4 (InterNetwork) để tránh Timeout IPv6 trên CachyOS
                var address = entry.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
                
                if (address == null) throw new HttpRequestException("Không tìm thấy địa chỉ IPv4 cho Wikipedia");

                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.NoDelay = true;

                try
                {
                    await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);
                    // Trả về NetworkStream chuẩn (Sử dụng System.Net.Sockets)
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            },
            ConnectTimeout = TimeSpan.FromSeconds(20),
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        };

        var httpClient = new HttpClient(handler);
        httpClient.Timeout = TimeSpan.FromSeconds(60); 

        httpClient.DefaultRequestHeaders.UserAgent.Clear();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");

        return httpClient;
    }
    public async Task<WikipediaSummaryResponse?> GetArticleSummaryAsync(string title, string language = "en")
    {
        using var httpClient = CreateHttpClient();
        try
        {
            // 1. QUAN TRỌNG: Wikipedia REST API yêu cầu tiêu đề dùng dấu gạch dưới thay vì khoảng trắng
            var formattedTitle = title.Replace(" ", "_");
            
            // 2. Escape tiêu đề để xử lý tiếng Việt có dấu
            var encodedTitle = Uri.EscapeDataString(formattedTitle);

            _logger.LogInformation("Fetching Wikipedia article: {Title} (Formatted: {FormattedTitle})", title, formattedTitle);

            var baseUrl = GetWikipediaApiBaseUrl(language);
            
            // Đảm bảo baseUrl kết thúc bằng dấu /
            if (!baseUrl.EndsWith("/")) baseUrl += "/";
            httpClient.BaseAddress = new Uri(baseUrl);

            // Gọi API Summary
            var response = await httpClient.GetAsync($"page/summary/{encodedTitle}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Wikipedia article not found: {Title} (URL: {Url}, Status: {Status})", 
                    title, httpClient.BaseAddress + $"page/summary/{encodedTitle}", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<WikipediaSummaryResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Wikipedia article: {Title}", title);
            throw;
        }
    }
    
    public async Task<WikipediaFullContentResponse?> GetArticleFullContentAsync(string title, string language = "vi")
    {
        using var httpClient = CreateHttpClient();
        var encodedTitle = Uri.EscapeDataString(title.Replace(" ", "_"));
        var url = $"https://{language}.wikipedia.org/api/rest_v1/page/mobile-html/{encodedTitle}";

        try 
        {
            _logger.LogInformation("Đang gửi request tải HTML: {Url}", url);
            
            // Sử dụng HttpCompletionOption.ResponseHeadersRead để tránh treo khi tải file nặng
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            
            // Kiểm tra mã trạng thái HTTP (404, 403, 500...)
            if (!response.IsSuccessStatusCode) 
            {
                _logger.LogWarning("Wikipedia từ chối (Status: {Status}). Có thể bị chặn User-Agent.", response.StatusCode);
                return null;
            }

            var htmlRaw = await response.Content.ReadAsStringAsync();

            return new WikipediaFullContentResponse
            {
                Title = title,
                Extract = "HTML Raw Content",
                FullContent = htmlRaw,
                Timestamp = DateTime.UtcNow.ToString("s")
            };
        }
        // 1. Bắt lỗi Timeout cụ thể
        catch (OperationCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            _logger.LogError("THẤT BẠI: Wikipedia không phản hồi sau {Timeout}s. (Có thể do IPv6 hoặc Firewall mạng nhà)", httpClient.Timeout.TotalSeconds);
            return null;
        }
        // 2. Bắt lỗi kết nối (DNS, No Internet, SSL...)
        catch (HttpRequestException ex)
        {
            _logger.LogError("LỖI KẾT NỐI: Không thể chạm tới Server Wikipedia. Chi tiết: {Msg}", ex.Message);
            return null;
        }
        // 3. Các lỗi không xác định khác
        catch (Exception ex) 
        {
            _logger.LogError(ex, "LỖI HỆ THỐNG: Có sự cố khi xử lý bài {Title}", title);
            return null;
        }
    }

    public async Task<List<WikipediaSearchResult>> SearchAsync(string query, string language = "vi", int limit = 10)
    {
        using var httpClient = CreateHttpClient();
        try
        {
            // Format query with underscores like Wikipedia URL
            var formattedQuery = query.Replace(" ", "_");
            var encodedQuery = Uri.EscapeDataString(formattedQuery);
            
            _logger.LogInformation("Searching Wikipedia: {Query} (Formatted: {FormattedQuery}, language: {Language})", query, formattedQuery, language);

            // Use the opensearch API for search - it's more reliable
            httpClient.BaseAddress = new Uri($"https://{language}.wikipedia.org/w/api.php");
            
            var response = await httpClient.GetAsync(
                $"action=opensearch&search={encodedQuery}&limit={limit}&namespace=0&format=json");
                
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Wikipedia search failed for: {Query} (Status: {Status})", query, response.StatusCode);
                return new List<WikipediaSearchResult>();
            }

            var json = await response.Content.ReadAsStringAsync();
            return ParseOpenSearchResults(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Wikipedia: {Query}", query);
            return new List<WikipediaSearchResult>();
        }
    }

    private string GetWikipediaApiBaseUrl(string language)
    {
        // Support both English and Vietnamese Wikipedia
        return language.ToLower() switch
        {
            "vi" => "https://vi.wikipedia.org/api/rest_v1",
            "en" => "https://en.wikipedia.org/api/rest_v1",
            _ => "https://en.wikipedia.org/api/rest_v1"
        };
    }

    private List<WikipediaSearchResult> ParseOpenSearchResults(string json)
    {
        try
        {
            // OpenSearch format: [query, [titles], [descriptions], [urls]]
            var results = new List<WikipediaSearchResult>();
            
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            if (root.ValueKind == System.Text.Json.JsonValueKind.Array && root.GetArrayLength() >= 2)
            {
                var titles = root[1];
                System.Text.Json.JsonElement descriptions = default;
                bool hasDescriptions = root.GetArrayLength() >= 3;

                if (hasDescriptions)
                {
                    descriptions = root[2];
                }

                int index = 0;
                foreach (var title in titles.EnumerateArray())
                {
                    string? desc = null;
                    if (hasDescriptions && descriptions.ValueKind == System.Text.Json.JsonValueKind.Array && index < descriptions.GetArrayLength())
                    {
                        desc = descriptions[index].GetString();
                    }
                    
                    var result = new WikipediaSearchResult
                    {
                        Title = title.GetString() ?? string.Empty,
                        Description = desc
                    };
                    results.Add(result);
                    index++;
                }
            }
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing OpenSearch results");
            return new List<WikipediaSearchResult>();
        }
    }
}


/// <summary>
/// Wikipedia search API response
/// </summary>
internal class WikipediaSearchResponse
{
    public List<WikipediaPageInfo>? Pages { get; set; }
}

internal class WikipediaPageInfo
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public WikipediaThumbnail? Thumbnail { get; set; }
}

internal class WikipediaThumbnail
{
    public string? Source { get; set; }
}

