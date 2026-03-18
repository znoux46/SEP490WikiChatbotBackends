using Microsoft.Extensions.Logging;
using WikiChatbotBackends.Application.DTOs;
using WikiChatbotBackends.Application.Interfaces;
using WikiChatbotBackends.Domain.Entities;
using System.Linq;

namespace WikiChatbotBackends.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(ICategoryRepository categoryRepository, ILogger<CategoryService> logger)
    {
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    public async Task<Guid> CreateAsync(CreateCategoryDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Name is required");

            if (await _categoryRepository.ExistsAsync(c => c.Name == dto.Name))
                throw new InvalidOperationException("Category name already exists");

            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };

            await _categoryRepository.AddAsync(category);
            return category.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            throw;
        }
    }

    public async Task UpdateAsync(Guid id, UpdateCategoryDto dto)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                throw new KeyNotFoundException($"Category {id} not found");

            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                if (await _categoryRepository.ExistsAsync(c => c.Name == dto.Name && c.Id != id))
                    throw new InvalidOperationException("Category name already exists");

                category.Name = dto.Name;
            }

            if (!string.IsNullOrWhiteSpace(dto.Description))
                category.Description = dto.Description;

            category.CreatedAt = DateTime.UtcNow;
            await _categoryRepository.UpdateAsync(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {Id}", id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                throw new KeyNotFoundException($"Category {id} not found");

            if (category.Details.Any())
                throw new InvalidOperationException("Cannot delete category with details");

            await _categoryRepository.DeleteAsync(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {Id}", id);
            throw;
        }
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        try
        {
            var categories = await _categoryRepository.GetAllWithDetailsAsync();

            return categories.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                DetailCount = c.Details.Count,
                CreatedAt = c.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all categories");
            throw;
        }
    }

    public async Task<List<CategoryListDto>> GetForLandingAsync(int limit = 10)
    {
        try
        {
            var categories = await _categoryRepository.GetForLandingAsync(limit);

            return categories.Select(c => new CategoryListDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                CreatedAt = c.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories for landing");
            throw;
        }
    }
}
