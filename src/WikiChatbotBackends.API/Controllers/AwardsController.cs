using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WikiChatbotBackends.API.Application.DTOs;
using WikiChatbotBackends.API.Application.Interfaces;

namespace WikiChatbotBackends.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AwardsController : ControllerBase
{
    private readonly IAwardService _awardService;

    public AwardsController(IAwardService awardService)
    {
        _awardService = awardService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AwardDto>>> GetAllAwards()
    {
        var awards = await _awardService.GetAllAwardsAsync();
        return Ok(awards);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<AwardDto>> CreateAward([FromBody] CreateAwardDto dto)
    {
        try
        {
            var award = await _awardService.CreateAwardAsync(dto);
            return CreatedAtAction(nameof(GetAllAwards), award);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<AwardDto>> UpdateAward(int id, [FromBody] UpdateAwardDto dto)
    {
        try
        {
            var award = await _awardService.UpdateAwardAsync(id, dto);
            return Ok(award);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteAward(int id)
    {
        try
        {
            await _awardService.DeleteAwardAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
