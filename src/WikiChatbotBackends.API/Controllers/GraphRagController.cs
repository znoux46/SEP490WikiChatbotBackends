using Microsoft.AspNetCore.Mvc;
using WikiChatbotBackends.Application.DTOs;
using WikiChatbotBackends.Application.Interfaces;

namespace WikiChatbotBackends.API.Controllers;

[ApiController]
[Route("api/graphrag")]
public class GraphRagController : ControllerBase
{
    private readonly IRagService _ragService;
    private readonly IWikipediaService _wikiService;
    private readonly ILogger<GraphRagController> _logger;

    public GraphRagController(IRagService ragService, IWikipediaService wikiService, ILogger<GraphRagController> logger)
    {
        _ragService = ragService;
        _wikiService = wikiService;
        _logger = logger;
    }

    /// <summary>
    /// Ingestion from Wikipedia name → GraphRAG /ingest_new (background)
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<JobStatusResponse>> Upload([FromForm] string target_person, [FromQuery] string language = "vi")
    {
        try
        {
            _logger.LogInformation("GraphRAG upload: {TargetPerson}", target_person);

            var wiki = await _wikiService.GetArticleSummaryAsync(target_person, language);
            if (wiki?.Extract == null)
                return BadRequest(new { message = "Wikipedia article not found" });

            var request = new GraphRagRequestDto 
            { 
                Text = wiki.Extract, 
                TargetPerson = target_person, 
                SourceType = "wiki"
            };

            var result = await _ragService.IngestNewAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GraphRAG upload failed");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Migrate from Postgres → GraphRAG /migrate_new
    /// </summary>
    [HttpPost("migrate/persons")]
    public async Task<ActionResult<JobStatusResponse>> Migrate([FromBody] GraphRagMigrateDto request)
    {
        try
        {
            _logger.LogInformation("GraphRAG migrate");
            var result = await _ragService.MigrateNewAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GraphRAG migrate failed");
            return StatusCode(500, new { message = ex.Message });
        }
    }
    
    [HttpGet("status/{jobId}")]
    public async Task<ActionResult<JobStatusResponse>> Status(string jobId)
    {
        var status = await _ragService.GetJobStatusAsync(jobId);
        if (status.Status == "not_found") return NotFound();
        return Ok(status);
    }

    [HttpGet("health")]
    public async Task<ActionResult> Health()
    {
        var healthy = await _ragService.HealthCheckAsync();
        return healthy ? Ok(new { status = "healthy" }) : StatusCode(503);
    }
}

