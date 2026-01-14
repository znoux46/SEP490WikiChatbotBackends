using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WikiChatbotBackends.API.Application.DTOs;
using WikiChatbotBackends.API.Application.Interfaces;

namespace WikiChatbotBackends.API.Controllers;

[ApiController]
[Route("api/people/award")]
[Authorize]
public class PeopleAwardController : ControllerBase
{
    private readonly IAwardService _awardService;

    public PeopleAwardController(IAwardService awardService)
    {
        _awardService = awardService;
    }

    [HttpPost]
    public async Task<IActionResult> AssignAward([FromBody] AssignAwardDto dto)
    {
        try
        {
            await _awardService.AssignAwardToPersonAsync(dto);
            return Ok(new { message = "Award assigned successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> RemoveAward([FromBody] RemoveAwardDto dto)
    {
        try
        {
            await _awardService.RemoveAwardFromPersonAsync(dto);
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
