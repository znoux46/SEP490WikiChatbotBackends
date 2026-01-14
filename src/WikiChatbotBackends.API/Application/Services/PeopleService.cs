using WikiChatbotBackends.API.Application.DTOs;
using WikiChatbotBackends.API.Application.Interfaces;
using WikiChatbotBackends.API.Domain.Entities;

namespace WikiChatbotBackends.API.Application.Services;

public class PeopleService : IPeopleService
{
    private readonly IRepository<NotablePerson> _personRepository;
    private readonly IRepository<PersonAward> _personAwardRepository;
    private readonly IRepository<PersonOrganization> _personOrganizationRepository;
    private readonly IRepository<PersonTag> _personTagRepository;
    private readonly IRepository<Award> _awardRepository;
    private readonly IRepository<Organization> _organizationRepository;
    private readonly IRepository<Tag> _tagRepository;

    public PeopleService(
        IRepository<NotablePerson> personRepository,
        IRepository<PersonAward> personAwardRepository,
        IRepository<PersonOrganization> personOrganizationRepository,
        IRepository<PersonTag> personTagRepository,
        IRepository<Award> awardRepository,
        IRepository<Organization> organizationRepository,
        IRepository<Tag> tagRepository)
    {
        _personRepository = personRepository;
        _personAwardRepository = personAwardRepository;
        _personOrganizationRepository = personOrganizationRepository;
        _personTagRepository = personTagRepository;
        _awardRepository = awardRepository;
        _organizationRepository = organizationRepository;
        _tagRepository = tagRepository;
    }

    public async Task<IEnumerable<NotablePersonDto>> GetAllPeopleAsync()
    {
        var people = await _personRepository.GetAllAsync();
        return people.Select(MapToDto);
    }

    public async Task<NotablePersonDto?> GetPersonByIdAsync(int id)
    {
        var person = await _personRepository.GetByIdAsync(id);
        if (person == null) return null;

        return await MapToDtoWithRelationsAsync(person);
    }

    public async Task<NotablePersonDto> CreatePersonAsync(CreateNotablePersonDto dto)
    {
        var person = new NotablePerson
        {
            Name = dto.Name,
            Biography = dto.Biography,
            DateOfBirth = dto.DateOfBirth,
            DateOfDeath = dto.DateOfDeath,
            Nationality = dto.Nationality,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdPerson = await _personRepository.AddAsync(person);

        // Assign tags
        if (dto.TagIds.Any())
        {
            foreach (var tagId in dto.TagIds)
            {
                var tag = await _tagRepository.GetByIdAsync(tagId);
                if (tag != null)
                {
                    var personTag = new PersonTag
                    {
                        PersonId = createdPerson.Id,
                        TagId = tagId,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _personTagRepository.AddAsync(personTag);
                }
            }
        }

        return await MapToDtoWithRelationsAsync(createdPerson);
    }

    public async Task<NotablePersonDto> UpdatePersonAsync(int id, UpdateNotablePersonDto dto)
    {
        var person = await _personRepository.GetByIdAsync(id);
        if (person == null)
            throw new KeyNotFoundException($"Person with id {id} not found");

        person.Name = dto.Name;
        person.Biography = dto.Biography;
        person.DateOfBirth = dto.DateOfBirth;
        person.DateOfDeath = dto.DateOfDeath;
        person.Nationality = dto.Nationality;
        person.UpdatedAt = DateTime.UtcNow;

        await _personRepository.UpdateAsync(person);
        return await MapToDtoWithRelationsAsync(person);
    }

    public async Task DeletePersonAsync(int id)
    {
        var person = await _personRepository.GetByIdAsync(id);
        if (person == null)
            throw new KeyNotFoundException($"Person with id {id} not found");

        // Delete related records
        var personAwards = await _personAwardRepository.FindAsync(pa => pa.PersonId == id);
        foreach (var pa in personAwards)
        {
            await _personAwardRepository.DeleteAsync(pa);
        }

        var personOrganizations = await _personOrganizationRepository.FindAsync(po => po.PersonId == id);
        foreach (var po in personOrganizations)
        {
            await _personOrganizationRepository.DeleteAsync(po);
        }

        var personTags = await _personTagRepository.FindAsync(pt => pt.PersonId == id);
        foreach (var pt in personTags)
        {
            await _personTagRepository.DeleteAsync(pt);
        }

        await _personRepository.DeleteAsync(person);
    }

    private NotablePersonDto MapToDto(NotablePerson person)
    {
        return new NotablePersonDto
        {
            Id = person.Id,
            Name = person.Name,
            Biography = person.Biography,
            DateOfBirth = person.DateOfBirth,
            DateOfDeath = person.DateOfDeath,
            Nationality = person.Nationality
        };
    }

    private async Task<NotablePersonDto> MapToDtoWithRelationsAsync(NotablePerson person)
    {
        var dto = MapToDto(person);

        // Load awards
        var personAwards = await _personAwardRepository.FindAsync(pa => pa.PersonId == person.Id);
        foreach (var pa in personAwards)
        {
            var award = await _awardRepository.GetByIdAsync(pa.AwardId);
            if (award != null)
            {
                dto.Awards.Add(new AwardDto
                {
                    Id = award.Id,
                    Name = award.Name,
                    Description = award.Description
                });
            }
        }

        // Load organizations
        var personOrganizations = await _personOrganizationRepository.FindAsync(po => po.PersonId == person.Id);
        foreach (var po in personOrganizations)
        {
            var organization = await _organizationRepository.GetByIdAsync(po.OrganizationId);
            if (organization != null)
            {
                dto.Organizations.Add(new OrganizationDto
                {
                    Id = organization.Id,
                    Name = organization.Name,
                    Description = organization.Description
                });
            }
        }

        // Load tags
        var personTags = await _personTagRepository.FindAsync(pt => pt.PersonId == person.Id);
        foreach (var pt in personTags)
        {
            var tag = await _tagRepository.GetByIdAsync(pt.TagId);
            if (tag != null)
            {
                dto.Tags.Add(new TagDto
                {
                    Id = tag.Id,
                    Name = tag.Name
                });
            }
        }

        return dto;
    }
}
