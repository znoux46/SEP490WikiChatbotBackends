using WikiChatbotBackends.API.Application.DTOs;
using WikiChatbotBackends.API.Application.Interfaces;
using WikiChatbotBackends.API.Domain.Entities;

namespace WikiChatbotBackends.API.Application.Services;

public class TagService : ITagService
{
    private readonly IRepository<Tag> _tagRepository;
    private readonly IRepository<PersonTag> _personTagRepository;

    public TagService(
        IRepository<Tag> tagRepository,
        IRepository<PersonTag> personTagRepository)
    {
        _tagRepository = tagRepository;
        _personTagRepository = personTagRepository;
    }

    public async Task<IEnumerable<TagDto>> GetAllTagsAsync()
    {
        var tags = await _tagRepository.GetAllAsync();
        return tags.Select(t => new TagDto
        {
            Id = t.Id,
            Name = t.Name
        });
    }

    public async Task<TagDto> CreateTagAsync(CreateTagDto dto)
    {
        var tag = new Tag
        {
            Name = dto.Name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdTag = await _tagRepository.AddAsync(tag);
        return new TagDto
        {
            Id = createdTag.Id,
            Name = createdTag.Name
        };
    }

    public async Task<TagDto> UpdateTagAsync(int id, UpdateTagDto dto)
    {
        var tag = await _tagRepository.GetByIdAsync(id);
        if (tag == null)
            throw new KeyNotFoundException($"Tag with id {id} not found");

        tag.Name = dto.Name;
        tag.UpdatedAt = DateTime.UtcNow;

        await _tagRepository.UpdateAsync(tag);
        return new TagDto
        {
            Id = tag.Id,
            Name = tag.Name
        };
    }

    public async Task DeleteTagAsync(int id)
    {
        var tag = await _tagRepository.GetByIdAsync(id);
        if (tag == null)
            throw new KeyNotFoundException($"Tag with id {id} not found");

        // Delete related PersonTag records
        var personTags = await _personTagRepository.FindAsync(pt => pt.TagId == id);
        foreach (var pt in personTags)
        {
            await _personTagRepository.DeleteAsync(pt);
        }

        await _tagRepository.DeleteAsync(tag);
    }
}
