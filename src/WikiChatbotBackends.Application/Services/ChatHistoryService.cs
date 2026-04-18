using Microsoft.AspNetCore.Http;
using WikiChatbotBackends.Application.DTOs;
using WikiChatbotBackends.Application.Interfaces;
using WikiChatbotBackends.Domain.Entities;

namespace WikiChatbotBackends.Application.Services;

public class ChatHistoryService : IChatHistoryService
{
    private readonly IRepository<ChatSession> _sessionRepository;
    private readonly IRepository<ChatHistory> _chatHistoryRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ChatHistoryService(
        IRepository<ChatSession> sessionRepository,
        IRepository<ChatHistory> chatHistoryRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _sessionRepository = sessionRepository;
        _chatHistoryRepository = chatHistoryRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    // Session methods
    public async Task<IEnumerable<SessionSummaryDto>> GetUserSessionsAsync(int userId)
    {
        var sessions = await _sessionRepository.FindAsync(s => s.UserId == userId);
        
        return sessions.Select(session => new SessionSummaryDto
        {
            SessionId = session.SessionId,
            SessionName = session.SessionName,
            ActivePerson = session.ActivePerson,
            CreatedAt = session.CreatedAt,
            LastMessageAt = session.UpdatedAt,
            ActivePerson = session.ActivePerson,
            MessageCount = session.ChatHistories.Count
        }).OrderByDescending(s => s.LastMessageAt);
    }

    public async Task<ChatSessionDto> GetSessionByIdAsync(int userId, Guid sessionId)
    {
        var sessions = await _sessionRepository.FindAsync(x => x.SessionId == sessionId);
        var session = sessions.FirstOrDefault();
        if (session == null || session.UserId != userId)
            throw new KeyNotFoundException($"Session with id {sessionId} not found");

        return new ChatSessionDto
        {
            SessionId = session.SessionId,
            SessionName = session.SessionName,
            ActivePerson = session.ActivePerson,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        };
    }

    public async Task<ChatSessionDto> CreateSessionAsync(int userId, CreateChatSessionDto dto)
    {
        var session = new ChatSession
        {
            UserId = userId,
            SessionId = dto.SessionId,
            SessionName = dto.SessionName,
            ActivePerson= dto.ActivePerson,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _sessionRepository.AddAsync(session);
        return new ChatSessionDto
        {
            SessionId = created.SessionId,
            SessionName = created.SessionName,
            ActivePerson = created.ActivePerson,
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt
        };
    }

    public async Task<ChatSessionDto> UpdateSessionAsync(int userId, Guid sessionId, UpdateChatSessionDto dto)
    {
        var sessions = await _sessionRepository.FindAsync(x=>x.SessionId==sessionId);
        var session = sessions.FirstOrDefault();
        if (session == null || session.UserId != userId)
            throw new KeyNotFoundException($"Session with id {sessionId} not found");

        session.SessionName = dto.SessionName;
        session.UpdatedAt = DateTime.UtcNow;

        await _sessionRepository.UpdateAsync(session);
        return new ChatSessionDto
        {
            SessionId = session.SessionId,
            SessionName = session.SessionName,
            ActivePerson = session.ActivePerson,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        };
    }

    public async Task DeleteSessionAsync(int userId, Guid sessionId)
    {
        var sessions = await _sessionRepository.FindAsync(x=>x.SessionId== sessionId);
        var session = sessions.FirstOrDefault();
        if (session == null || session.UserId != userId)
            throw new KeyNotFoundException($"Session with id {sessionId} not found");

        // Xóa tất cả chat histories trước (hoặc để cascade delete)
        var histories = await _chatHistoryRepository.FindAsync(ch => ch.SessionId == sessionId);
        foreach (var history in histories)
        {
            await _chatHistoryRepository.DeleteAsync(history);
        }

        await _sessionRepository.DeleteAsync(session);
    }

    public async Task ClearAllSessionsAsync(int userId)
    {
        var sessions = await _sessionRepository.FindAsync(s => s.UserId == userId);
        foreach (var session in sessions)
        {
            // Xóa histories trước
            var histories = await _chatHistoryRepository.FindAsync(ch => ch.SessionId == session.SessionId);
            foreach (var history in histories)
            {
                await _chatHistoryRepository.DeleteAsync(history);
            }
            await _sessionRepository.DeleteAsync(session);
        }
    }

    // History methods
    public async Task<IEnumerable<ChatHistoryDto>> GetSessionHistoryAsync(int userId, Guid sessionId)
    {
        // Verify session belongs to user
        var sessions = await _sessionRepository.FindAsync(x=>x.SessionId==sessionId);
        var session = sessions.FirstOrDefault();
        if (session == null || session.UserId != userId)
            throw new KeyNotFoundException($"Session with id {sessionId} not found");

        var histories = await _chatHistoryRepository.FindAsync(ch => ch.SessionId == sessionId);
        return histories.OrderBy(ch => ch.CreatedAt).Select(ch => new ChatHistoryDto
        {
            Id = ch.Id,
            SessionId = ch.SessionId,
            Question = ch.Question,
            Answer = ch.Answer,
            AIModel = ch.AIModel,
            CreatedAt = ch.CreatedAt
        });
    }

    public async Task<ChatHistoryDto> CreateChatHistoryAsync(int userId, CreateChatHistoryDto dto)
    {
        // Verify session belongs to user
        var sessions = await _sessionRepository.FindAsync(x => x.SessionId == dto.SessionId);
        var session = sessions.FirstOrDefault();
        if (session == null || session.UserId != userId)
            throw new KeyNotFoundException($"Session with id {dto.SessionId} not found");

        var chatHistory = new ChatHistory
        {
            SessionId = dto.SessionId,
            Question = dto.Question,
            Answer = dto.Answer,
            AIModel = dto.AIModel,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _chatHistoryRepository.AddAsync(chatHistory);
        
        // Update session UpdatedAt
        session.UpdatedAt = DateTime.UtcNow;
        await _sessionRepository.UpdateAsync(session);

        return new ChatHistoryDto
        {
            Id = created.Id,
            SessionId = created.SessionId,
            Question = created.Question,
            Answer = created.Answer,
            AIModel = created.AIModel,
            CreatedAt = created.CreatedAt
        };
    }

    public async Task<ChatHistoryDto> UpdateChatHistoryAsync(int userId, int historyId, UpdateChatHistoryDto dto)
    {
        var history = await _chatHistoryRepository.GetByIdAsync(historyId);
        if (history == null)
            throw new KeyNotFoundException($"Chat history with id {historyId} not found");

        // Verify session belongs to user
        var sessions = await _sessionRepository.FindAsync(x => x.SessionId == history.SessionId);
        var session = sessions.FirstOrDefault();
        if (session == null || session.UserId != userId)
            throw new UnauthorizedAccessException("You don't have permission to update this history");

        history.Question = dto.Question;
        history.Answer = dto.Answer;
        history.AIModel = dto.AIModel;
        history.UpdatedAt = DateTime.UtcNow;

        await _chatHistoryRepository.UpdateAsync(history);
        return new ChatHistoryDto
        {
            Id = history.Id,
            SessionId = history.SessionId,
            Question = history.Question,
            Answer = history.Answer,
            AIModel = history.AIModel,
            CreatedAt = history.CreatedAt
        };
    }

    public async Task DeleteChatHistoryAsync(int userId, int historyId)
    {
        var history = await _chatHistoryRepository.GetByIdAsync(historyId);
        if (history == null)
            throw new KeyNotFoundException($"Chat history with id {historyId} not found");

        // Verify session belongs to user
        var sessions = await _sessionRepository.FindAsync(x => x.SessionId == history.SessionId);
        var session = sessions.FirstOrDefault();
        if (session == null || session.UserId != userId)
            throw new UnauthorizedAccessException("You don't have permission to delete this history");

        await _chatHistoryRepository.DeleteAsync(history);
    }

    /// <summary>
    /// Tự động lấy thông tin từ HttpContext để lưu lịch sử Chat
    /// </summary>
    public async Task<string> SaveChatHistoryWithContextAsync(string question, string answer,string AIModel, string ActivePerson, Guid sessionId)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        var sessionIdString = (sessionId != Guid.Empty) ? sessionId : Guid.NewGuid();

        if (httpContext == null) return sessionIdString.ToString();


        int userId = 0;
        // 1. Lấy UserId từ Claims (nếu không có thì mặc định là 0 hoặc xử lý tùy ý)
        var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int parsedId))
        {
            // Nếu bạn muốn cho phép lưu lịch sử cho khách (Anonymous), 
            // bạn cần một UserId mặc định hoặc bỏ qua logic này.
            // userId = 0;

            userId = parsedId;
        }

        // var sessionIdString = (sessionId != Guid.Empty) ? sessionId : Guid.NewGuid();

        try
        {
            // 2. Lấy SessionId (GUID string) từ Header hoặc tạo mới
            // var sessionIdString = sessionId!=Guid.Empty ? sessionId: Guid.NewGuid();

            // 3. Tìm hoặc tạo Session dựa trên SessionId (string) và UserId
            var sessions = await _sessionRepository.FindAsync(s => s.UserId == userId && s.SessionId == sessionIdString);
            var session = sessions.FirstOrDefault();

            if (session == null)
            {
                // Tạo session mới nếu chưa tồn tại
                // var newSession = new ChatSession
                // {
                //     UserId = userId,
                //     SessionId = sessionIdString,
                //     ActivePerson = ActivePerson,
                //     SessionName = question.Length > 30 ? question.Substring(0, 27) + "..." : question,
                //     CreatedAt = DateTime.UtcNow,
                //     UpdatedAt = DateTime.UtcNow
                // };
                // var created = await _sessionRepository.AddAsync(newSession);

                await _sessionRepository.AddAsync(new ChatSession
                {
                    UserId = userId, // Sẽ là null nếu là Anonymous
                    SessionId = sessionIdString,
                    ActivePerson = ActivePerson,
                    SessionName = question.Length > 30 ? question.Substring(0, 27) + "..." : question,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

            }
            else
            {
                // Cập nhật thời gian cho Session
                session.UpdatedAt = DateTime.UtcNow;
                session.ActivePerson = ActivePerson; // Cập nhật ActivePerson nếu cần
                await _sessionRepository.UpdateAsync(session);
            }

            // // 4. Lưu vào ChatHistory
            // var history = new ChatHistory
            // {
            //     SessionId = sessionIdString,
            //     Question = question,
            //     Answer = answer,
            //     AIModel = AIModel,
            //     CreatedAt = DateTime.UtcNow,
            //     UpdatedAt = DateTime.UtcNow
            // };

            // await _chatHistoryRepository.AddAsync(history);

            // return sessionIdString.ToString(); // Trả về SessionId để client có thể sử dụng cho các lần gọi tiếp theo
        }
        catch (Exception ex)
        {
            // Log lỗi nhưng không làm gián đoạn luồng trả lời của AI
            // _logger.LogError(ex, "Failed to save history in service");
            // return string.Empty;

            // Bước 2: Log lỗi để em biết DB đang bị gì (Double Insert, Connection...)
            // _logger.LogError(ex, "Lưu lịch sử thất bại cho User {UserId}, Session {SessionId}", userId, sessionIdString);

            // Bước 3: QUAN TRỌNG NHẤT
            // Dù lưu DB thất bại, ta vẫn trả về sessionIdString mà ta đã tạo ở trên.
            // Frontend sẽ nhận được ID này, coi như "phiên chat tạm" và không báo lỗi UI.
        }

        return sessionIdString.ToString();

    }
}