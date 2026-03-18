using Microsoft.Extensions.Logging;
using WikiChatbotBackends.Application.DTOs;
using WikiChatbotBackends.Application.Interfaces;
using WikiChatbotBackends.Domain.Entities;
using System.Linq;

namespace WikiChatbotBackends.Application.Services;

public class DetailService : IDetailService
{
    private readonly IDetailRepository _detailRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<DetailService> _logger;

    public DetailService(IDetailRepository detailRepository, ICategoryRepository categoryRepository, ILogger<DetailService> logger)
    {
        _detailRepository = detailRepository;
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    public async Task<Guid> CreateAsync(CreateDetailDto dto)
    {
        try
        {
            // Check category exists
            if (!await _categoryRepository.ExistsByIdAsync(dto.CategoryId))
                throw new KeyNotFoundException($"Category {dto.CategoryId} not found");

            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("Title is required");

            var detail = new Detail
            {
                Id = Guid.NewGuid(),
                CategoryId = dto.CategoryId,
                Title = dto.Title,
                Content = dto.Content,
                WikipediaUrl = dto.WikipediaUrl,
                CreatedAt = DateTime.UtcNow
            };

            await _detailRepository.AddAsync(detail);
            return detail.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating detail");
            throw;
        }
    }

    public async Task UpdateAsync(Guid id, UpdateDetailDto dto)
    {
        try
        {
            var detail = await _detailRepository.GetByIdAsync(id);
            if (detail == null)
                throw new KeyNotFoundException($"Detail {id} not found");

            if (!string.IsNullOrWhiteSpace(dto.Title))
                detail.Title = dto.Title;

            if (!string.IsNullOrWhiteSpace(dto.Content))
                detail.Content = dto.Content;

            detail.WikipediaUrl = dto.WikipediaUrl;
            await _detailRepository.UpdateAsync(detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating detail {Id}", id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            var detail = await _detailRepository.GetByIdAsync(id);
            if (detail == null)
                throw new KeyNotFoundException($"Detail {id} not found");

            await _detailRepository.DeleteAsync(detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting detail {Id}", id);
            throw;
        }
    }

    public async Task<DetailDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var detail = await _detailRepository.GetByIdWithCategoryAsync(id);

            if (detail == null) return null;

            return new DetailDto
            {
                Id = detail.Id,
                Title = detail.Title,
                Content = detail.Content,
                WikipediaUrl = detail.WikipediaUrl,
                CategoryId = detail.CategoryId,
                CategoryName = detail.Category.Name,
                CreatedAt = detail.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting detail by id {Id}", id);
            throw;
        }
    }

    public async Task<List<DetailDto>> GetByCategoryIdAsync(Guid categoryId)
    {
        try
        {
            var details = await _detailRepository.GetByCategoryIdWithCategoryAsync(categoryId);

            return details.Select(d => new DetailDto
            {
                Id = d.Id,
                Title = d.Title,
                Content = d.Content,
                WikipediaUrl = d.WikipediaUrl,
                CategoryId = d.CategoryId,
                CategoryName = d.Category.Name,
                CreatedAt = d.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting details by category {CategoryId}", categoryId);
            throw;
        }
    }

    public async Task<List<DetailDto>> GetAllAsync()
    {
        try
        {
            var categories = await _categoryRepository.GetAllWithDetailsAsync();
            var allDetails = new List<DetailDto>();
            foreach (var cat in categories)
            {
                var details = cat.Details.Select(d => new DetailDto
                {
                    Id = d.Id,
                    Title = d.Title,
                    Content = d.Content,
                    WikipediaUrl = d.WikipediaUrl,
                    CategoryId = d.CategoryId,
                    CategoryName = cat.Name,
                    CreatedAt = d.CreatedAt
                }).ToList();
                allDetails.AddRange(details);
            }
            return allDetails.OrderBy(d => d.CategoryName).ThenBy(d => d.Title).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all details");
            throw;
        }
    }
}
