using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WikiChatbotBackends.API.Application.DTOs;
using WikiChatbotBackends.API.Application.Interfaces;

namespace WikiChatbotBackends.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PeopleController : ControllerBase
{
    private readonly IPeopleService _peopleService;

    public PeopleController(IPeopleService peopleService)
    {
        _peopleService = peopleService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotablePersonDto>>> GetAllPeople()
    {
        var people = await _peopleService.GetAllPeopleAsync();
        return Ok(people);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<NotablePersonDto>> GetPersonById(int id)
    {
        var person = await _peopleService.GetPersonByIdAsync(id);
        if (person == null)
            return NotFound(new { message = $"Person with id {id} not found" });

        return Ok(person);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<NotablePersonDto>> CreatePerson([FromBody] CreateNotablePersonDto dto)
    {
        try
        {
            var person = await _peopleService.CreatePersonAsync(dto);
            return CreatedAtAction(nameof(GetPersonById), new { id = person.Id }, person);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<NotablePersonDto>> UpdatePerson(int id, [FromBody] UpdateNotablePersonDto dto)
    {
        try
        {
            var person = await _peopleService.UpdatePersonAsync(id, dto);
            return Ok(person);
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
    public async Task<IActionResult> DeletePerson(int id)
    {
        try
        {
            await _peopleService.DeletePersonAsync(id);
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
