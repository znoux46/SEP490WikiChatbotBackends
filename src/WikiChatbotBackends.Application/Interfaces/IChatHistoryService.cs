using WikiChatbotBackends.Application.DTOs;

namespace WikiChatbotBackends.Application.Interfaces;

public interface IChatHistoryService
{
    Task<IEnumerable<SessionSummaryDto>> GetUserSessionsAsync(int userId);
    Task<IEnumerable<ChatHistoryDto>> GetSessionHistoryAsync(int userId, string sessionId);
    Task<ChatHistoryDto> CreateChatHistoryAsync(int userId, CreateChatHistoryDto dto);
    Task<ChatHistoryDto> UpdateChatHistoryAsync(int userId, int historyId, UpdateChatHistoryDto dto);
    Task DeleteSessionAsync(int userId, string sessionId);
    Task ClearAllHistoryAsync(int userId);
}
