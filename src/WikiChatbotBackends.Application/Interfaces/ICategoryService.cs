using WikiChatbotBackends.Application.DTOs;
using System;
using System.Threading.Tasks;

namespace WikiChatbotBackends.Application.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();
    Task<List<CategoryListDto>> GetForLandingAsync(int limit = 10);
    Task<Guid> CreateAsync(CreateCategoryDto dto);
    Task UpdateAsync(Guid id, UpdateCategoryDto dto);
    Task DeleteAsync(Guid id);
}
