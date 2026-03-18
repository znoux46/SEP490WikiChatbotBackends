using WikiChatbotBackends.Application.DTOs;
using System;
using System.Threading.Tasks;

namespace WikiChatbotBackends.Application.Interfaces;

public interface IDetailService
{
    Task<DetailDto?> GetByIdAsync(Guid id);
    Task<List<DetailDto>> GetByCategoryIdAsync(Guid categoryId);
    Task<List<DetailDto>> GetAllAsync();
    Task<Guid> CreateAsync(CreateDetailDto dto);
    Task UpdateAsync(Guid id, UpdateDetailDto dto);
    Task DeleteAsync(Guid id);
}
