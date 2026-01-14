using WikiChatbotBackends.API.Application.DTOs;

namespace WikiChatbotBackends.API.Application.Interfaces;

public interface ITagService
{
    Task<IEnumerable<TagDto>> GetAllTagsAsync();
    Task<TagDto> CreateTagAsync(CreateTagDto dto);
    Task<TagDto> UpdateTagAsync(int id, UpdateTagDto dto);
    Task DeleteTagAsync(int id);
}
