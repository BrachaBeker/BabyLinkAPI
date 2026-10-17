// ממשק שירות תגיות: GetAllAsync - לרשימת הסינון בלקוח.
using BabyLink.Application.DTOs.Tags;

namespace BabyLink.Application.Interfaces.Services;

public interface ITagService
{
    Task<IReadOnlyList<TagDto>> GetAllAsync(CancellationToken cancellationToken);
}
