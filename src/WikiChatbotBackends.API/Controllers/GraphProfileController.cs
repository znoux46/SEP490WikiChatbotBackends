using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace WikiChatbotBackends.API.Controllers;

[ApiController]
[Route("api/graphprofile")]
public class GraphProfileController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GraphProfileController> _logger;

private readonly IConfiguration _configuration;
public GraphProfileController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<GraphProfileController> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
_httpClient.BaseAddress = new Uri(_configuration["RagService:BaseUrl"] ?? "http://localhost:8000");
        _logger = logger;
    }

    /// <summary>
    /// 1. Data Ingestion - name → wiki → extract → Neo4j (multipart/form-data)
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult> Upload([FromForm] string target_person)
    {
        try
        {
            _logger.LogInformation("GraphProfile upload: {TargetPerson}", target_person);

            var requestObj = new 
            {
                text = "", // Will fetch wiki in Python
                target_person,
                source_type = "wiki"
            };

            var response = await _httpClient.PostAsJsonAsync("/ingest_new", requestObj);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            return Ok(new 
            {
                job_id = result.GetProperty("job_id").GetString(),
                status = "queued", 
                target_person
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GraphProfile upload failed");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// 2. Direct Extraction (no upload)
    /// </summary>
    [HttpPost("extract-direct")]
    public async Task<ActionResult> ExtractDirect([FromForm] string text, [FromForm] string target_person)
    {
        var requestObj = new 
        {
            text,
            target_person
        };
        var response = await _httpClient.PostAsJsonAsync("/extract-direct", requestObj);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        return Ok(result);
    }

    /// <summary>
    /// Status check
    /// </summary>
    [HttpGet("status/{jobId}")]
    public async Task<ActionResult> Status(string jobId)
    {
        var response = await _httpClient.GetAsync($"/status/{jobId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) 
            return NotFound(new { message = "Job not found" });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        return Ok(result);
    }

    /// <summary>
    /// 3. Health & Monitoring
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult> Health()
    {
        var response = await _httpClient.GetAsync("/health");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        return Ok(result);
    }

    /// <summary>
    /// 4. Migration PG → Neo4j
    /// </summary>
    [HttpPost("migrate/persons")]
    public async Task<ActionResult> Migrate([FromBody] object request)
    {
        var response = await _httpClient.PostAsJsonAsync("/migrate_new", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        return Ok(result);
    }
}

