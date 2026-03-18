using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WikiChatbotBackends.Application.DTOs;
using WikiChatbotBackends.Application.Interfaces;
using System;

namespace WikiChatbotBackends.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DetailController : ControllerBase
    {
        private readonly IDetailService _detailService;
        private readonly ILogger<DetailController> _logger;

        public DetailController(IDetailService detailService, ILogger<DetailController> logger)
        {
            _detailService = detailService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy detail theo ID
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(DetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DetailDto>> GetById(Guid id)
        {
            try
            {
                _logger.LogInformation("Getting detail {Id}", id);
                var detail = await _detailService.GetByIdAsync(id);
                if (detail == null)
                {
                    return NotFound(new { message = $"Detail không tìm thấy: {id}" });
                }
                return Ok(detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting detail {Id}", id);
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy details theo category ID
        /// </summary>
        [HttpGet("category/{categoryId}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<DetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<DetailDto>>> GetByCategoryId(Guid categoryId)
        {
            try
            {
                _logger.LogInformation("Getting details for category {CategoryId}", categoryId);
                var details = await _detailService.GetByCategoryIdAsync(categoryId);
                return Ok(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting details for category {CategoryId}", categoryId);
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
            }
        }
    }
}
