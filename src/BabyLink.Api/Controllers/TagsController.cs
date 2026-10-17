// GET /api/tags - רשימת התגיות לסינון (דורש התחברות) -> 200.
using BabyLink.Application.DTOs.Tags;
using BabyLink.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabyLink.Api.Controllers;

[ApiController]
[Route("api/tags")]
[Authorize]
[Produces("application/json")]
public class TagsController(ITagService tagService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TagDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<TagDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await tagService.GetAllAsync(cancellationToken));
    }
}
