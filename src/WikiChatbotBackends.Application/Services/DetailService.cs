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

            var document = new Document
            {
                Id = Guid.NewGuid(),
                CategoryId = dto.CategoryId,
                FileName = dto.Title,
                Description = dto.Content,
                Status = "active",
                SourceType = "manual",
                FilePath = "",
                FileSize = 0,
                ContentHash = "",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _detailRepository.AddAsync(document);
            return document.Id;
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
            var document = await _detailRepository.GetByIdAsync(id);
            if (document == null)
                throw new KeyNotFoundException($"Document {id} not found");

            if (!string.IsNullOrWhiteSpace(dto.Title))
                document.FileName = dto.Title;

            if (!string.IsNullOrWhiteSpace(dto.Content))
                document.Description = dto.Content;

            document.UpdatedAt = DateTime.UtcNow;
            await _detailRepository.UpdateAsync(document);
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
            var document = await _detailRepository.GetByIdAsync(id);
            if (document == null)
                throw new KeyNotFoundException($"Document {id} not found");

            await _detailRepository.DeleteAsync(document);
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
            var document = await _detailRepository.GetByIdWithCategoryAsync(id);

            if (document == null) return null;

            return new DetailDto
            {
                Id = document.Id,
                Title = document.FileName,
                Content = document.Description ?? string.Empty,
                WikipediaUrl = null,
                CategoryId = document.CategoryId ?? Guid.Empty,
                CategoryName = string.Empty,
                CreatedAt = document.CreatedAt
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
            var documents = await _detailRepository.GetByCategoryIdWithCategoryAsync(categoryId);

            return documents.Select(d => new DetailDto
            {
                Id = d.Id,
                Title = d.FileName,
                Content = d.Description ?? string.Empty,
                WikipediaUrl = null,
                CategoryId = d.CategoryId ?? Guid.Empty,
                CategoryName = string.Empty,
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
                var documents = cat.Documents.Select(d => new DetailDto
                {
                    Id = d.Id,
                    Title = d.FileName,
                    Content = d.Description ?? string.Empty,
                    WikipediaUrl = null,
                    CategoryId = d.CategoryId ?? Guid.Empty,
                    CategoryName = cat.Name,
                    CreatedAt = d.CreatedAt
                }).ToList();
                allDetails.AddRange(documents);
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
