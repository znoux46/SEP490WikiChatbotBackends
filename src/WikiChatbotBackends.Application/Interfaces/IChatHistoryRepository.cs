using System.Linq.Expressions;
using WikiChatbotBackends.Domain.Entities;

namespace WikiChatbotBackends.Application.Interfaces;

public interface IChatHistoryRepository : IRepository<ChatHistory>
{
    Task<IEnumerable<ChatHistory>> GetChatHistoriesAsync(
        Expression<Func<ChatHistory, bool>>? predicate = null,
        Func<IQueryable<ChatHistory>, IOrderedQueryable<ChatHistory>>? orderBy = null,
        int? skip = null,
        int? take = null);
    
    Task<int> CountChatHistoriesAsync(Expression<Func<ChatHistory, bool>>? predicate = null);
    
    Task<int> GetNewMessagesCountAsync(DateTime startDate, DateTime endDate);
}
