using WikiChatbotBackends.API.Application.DTOs;

namespace WikiChatbotBackends.API.Application.Interfaces;

public interface IAwardService
{
    Task<IEnumerable<AwardDto>> GetAllAwardsAsync();
    Task<AwardDto> CreateAwardAsync(CreateAwardDto dto);
    Task<AwardDto> UpdateAwardAsync(int id, UpdateAwardDto dto);
    Task DeleteAwardAsync(int id);
    Task AssignAwardToPersonAsync(AssignAwardDto dto);
    Task RemoveAwardFromPersonAsync(RemoveAwardDto dto);
}
