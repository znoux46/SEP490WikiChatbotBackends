using WikiChatbotBackends.Application.DTOs;

namespace WikiChatbotBackends.Application.Interfaces;

public interface IChatHistoryService
{
    // Session methods
    Task<IEnumerable<SessionSummaryDto>> GetUserSessionsAsync(int userId);
    Task<ChatSessionDto> GetSessionByIdAsync(int userId, int sessionId);
    Task<ChatSessionDto> CreateSessionAsync(int userId, CreateChatSessionDto dto);
    Task<ChatSessionDto> UpdateSessionAsync(int userId, int sessionId, UpdateChatSessionDto dto);
    Task DeleteSessionAsync(int userId, int sessionId);
    Task ClearAllSessionsAsync(int userId);
    
    // History methods
    Task<IEnumerable<ChatHistoryDto>> GetSessionHistoryAsync(int userId, int sessionId);
    Task<ChatHistoryDto> CreateChatHistoryAsync(int userId, CreateChatHistoryDto dto);
    Task<ChatHistoryDto> UpdateChatHistoryAsync(int userId, int historyId, UpdateChatHistoryDto dto);
    Task DeleteChatHistoryAsync(int userId, int historyId);
}
