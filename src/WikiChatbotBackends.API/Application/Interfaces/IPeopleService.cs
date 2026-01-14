using WikiChatbotBackends.API.Application.DTOs;

namespace WikiChatbotBackends.API.Application.Interfaces;

public interface IPeopleService
{
    Task<IEnumerable<NotablePersonDto>> GetAllPeopleAsync();
    Task<NotablePersonDto?> GetPersonByIdAsync(int id);
    Task<NotablePersonDto> CreatePersonAsync(CreateNotablePersonDto dto);
    Task<NotablePersonDto> UpdatePersonAsync(int id, UpdateNotablePersonDto dto);
    Task DeletePersonAsync(int id);
}
