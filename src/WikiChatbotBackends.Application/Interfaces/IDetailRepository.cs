using WikiChatbotBackends.Application.Interfaces;
using WikiChatbotBackends.Domain.Entities;
using System;
using System.Linq.Expressions;

namespace WikiChatbotBackends.Application.Interfaces;

public interface IDetailRepository : IRepository<Document>
{
    Task<Document?> GetByIdWithCategoryAsync(Guid id);
    Task<List<Document>> GetByCategoryIdWithCategoryAsync(Guid categoryId);
    Task<Document?> GetByIdAsync(Guid id);
    Task<bool> ExistsAsync(Expression<Func<Document, bool>> predicate);
}
