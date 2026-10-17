// מימוש ITagService - שליפת כל התגיות ומיפוי ל-TagDto.
using AutoMapper;
using BabyLink.Application.DTOs.Tags;
using BabyLink.Application.Interfaces.Repositories;
using BabyLink.Application.Interfaces.Services;

namespace BabyLink.Application.Services;

public class TagService(ITagRepository repository, IMapper mapper) : ITagService
{
    public async Task<IReadOnlyList<TagDto>> GetAllAsync(CancellationToken cancellationToken) =>
        mapper.Map<List<TagDto>>(await repository.GetAllAsync(cancellationToken));
}
