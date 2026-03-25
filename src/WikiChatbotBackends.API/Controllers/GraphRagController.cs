using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace WikiChatbotBackends.API.Controllers;

[ApiController]
[Route("api/graphrag")]
public class GraphRagController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GraphRagController> _logger;

private readonly IConfiguration _configuration;
public GraphRagController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<GraphRagController> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
_httpClient.BaseAddress = new Uri(_configuration["RagService:BaseUrl"] ?? "http://localhost:8000");
        _logger = logger;
    }

    /// <summary>
    /// 3. POST /chat - Graph RAG Query
    /// </summary>
    [HttpPost("chat")]
    public async Task<ActionResult> Chat([FromBody] object request)
    {
        try
        {
            _logger.LogInformation("GraphRAG chat");
            var response = await _httpClient.PostAsJsonAsync("/chat", request);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GraphRAG chat failed");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// 1. POST /ingest_new proxy
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult> Upload([FromBody] object request)
    {
        var response = await _httpClient.PostAsJsonAsync("/ingest_new", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        return Ok(result);
    }

    /// <summary>
    /// 2. POST /migrate_new proxy
    /// </summary>
    [HttpPost("migrate/persons")]
    public async Task<ActionResult> Migrate([FromBody] object request)
    {
        var response = await _httpClient.PostAsJsonAsync("/migrate_new", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        return Ok(result);
    }

    [HttpGet("status/{jobId}")]
    public async Task<ActionResult> Status(string jobId)
    {
        var response = await _httpClient.GetAsync($"/status/{jobId}");
        if (!response.IsSuccessStatusCode) return NotFound();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        return Ok(result);
    }

    [HttpGet("health")]
    public async Task<ActionResult> Health()
    {
        var response = await _httpClient.GetAsync("/health");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        return Ok(result);
    }
}

