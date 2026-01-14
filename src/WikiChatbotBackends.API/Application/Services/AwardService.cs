using WikiChatbotBackends.API.Application.DTOs;
using WikiChatbotBackends.API.Application.Interfaces;
using WikiChatbotBackends.API.Domain.Entities;

namespace WikiChatbotBackends.API.Application.Services;

public class AwardService : IAwardService
{
    private readonly IRepository<Award> _awardRepository;
    private readonly IRepository<PersonAward> _personAwardRepository;
    private readonly IRepository<NotablePerson> _personRepository;

    public AwardService(
        IRepository<Award> awardRepository,
        IRepository<PersonAward> personAwardRepository,
        IRepository<NotablePerson> personRepository)
    {
        _awardRepository = awardRepository;
        _personAwardRepository = personAwardRepository;
        _personRepository = personRepository;
    }

    public async Task<IEnumerable<AwardDto>> GetAllAwardsAsync()
    {
        var awards = await _awardRepository.GetAllAsync();
        return awards.Select(a => new AwardDto
        {
            Id = a.Id,
            Name = a.Name,
            Description = a.Description
        });
    }

    public async Task<AwardDto> CreateAwardAsync(CreateAwardDto dto)
    {
        var award = new Award
        {
            Name = dto.Name,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdAward = await _awardRepository.AddAsync(award);
        return new AwardDto
        {
            Id = createdAward.Id,
            Name = createdAward.Name,
            Description = createdAward.Description
        };
    }

    public async Task<AwardDto> UpdateAwardAsync(int id, UpdateAwardDto dto)
    {
        var award = await _awardRepository.GetByIdAsync(id);
        if (award == null)
            throw new KeyNotFoundException($"Award with id {id} not found");

        award.Name = dto.Name;
        award.Description = dto.Description;
        award.UpdatedAt = DateTime.UtcNow;

        await _awardRepository.UpdateAsync(award);
        return new AwardDto
        {
            Id = award.Id,
            Name = award.Name,
            Description = award.Description
        };
    }

    public async Task DeleteAwardAsync(int id)
    {
        var award = await _awardRepository.GetByIdAsync(id);
        if (award == null)
            throw new KeyNotFoundException($"Award with id {id} not found");

        // Delete related PersonAward records
        var personAwards = await _personAwardRepository.FindAsync(pa => pa.AwardId == id);
        foreach (var pa in personAwards)
        {
            await _personAwardRepository.DeleteAsync(pa);
        }

        await _awardRepository.DeleteAsync(award);
    }

    public async Task AssignAwardToPersonAsync(AssignAwardDto dto)
    {
        var person = await _personRepository.GetByIdAsync(dto.PersonId);
        if (person == null)
            throw new KeyNotFoundException($"Person with id {dto.PersonId} not found");

        var award = await _awardRepository.GetByIdAsync(dto.AwardId);
        if (award == null)
            throw new KeyNotFoundException($"Award with id {dto.AwardId} not found");

        var existing = await _personAwardRepository.FindAsync(pa => pa.PersonId == dto.PersonId && pa.AwardId == dto.AwardId);
        if (existing.Any())
            throw new InvalidOperationException("Award already assigned to this person");

        var personAward = new PersonAward
        {
            PersonId = dto.PersonId,
            AwardId = dto.AwardId,
            AwardedAt = dto.AwardedAt,
            CreatedAt = DateTime.UtcNow
        };

        await _personAwardRepository.AddAsync(personAward);
    }

    public async Task RemoveAwardFromPersonAsync(RemoveAwardDto dto)
    {
        var personAwards = await _personAwardRepository.FindAsync(pa => pa.PersonId == dto.PersonId && pa.AwardId == dto.AwardId);
        var personAward = personAwards.FirstOrDefault();
        
        if (personAward == null)
            throw new KeyNotFoundException("Award assignment not found");

        await _personAwardRepository.DeleteAsync(personAward);
    }
}
