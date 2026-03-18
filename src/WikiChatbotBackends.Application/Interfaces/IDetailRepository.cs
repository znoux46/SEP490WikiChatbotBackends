using WikiChatbotBackends.Application.Interfaces;
using WikiChatbotBackends.Domain.Entities;
using System;
using System.Linq.Expressions;

namespace WikiChatbotBackends.Application.Interfaces;

public interface IDetailRepository : IRepository<Detail>
{
    Task<Detail?> GetByIdWithCategoryAsync(Guid id);
    Task<List<Detail>> GetByCategoryIdWithCategoryAsync(Guid categoryId);
    Task<Detail?> GetByIdAsync(Guid id);
    Task<bool> ExistsAsync(Expression<Func<Detail, bool>> predicate);
}
