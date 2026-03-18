using WikiChatbotBackends.Application.Interfaces;
using WikiChatbotBackends.Domain.Entities;
using System;

namespace WikiChatbotBackends.Application.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    Task<Category?> GetByIdAsync(Guid id);
    Task<bool> ExistsAsync(string name);
    Task<bool> ExistsByIdAsync(Guid id);
    Task<List<Category>> GetAllWithDetailsAsync();
    Task<List<Category>> GetForLandingAsync(int limit);
}
