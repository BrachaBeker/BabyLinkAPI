// /api/notifications - ההודעות של המשתמש המחובר (למשל: ההורה ביטל בקשה שתפסת).
// GET - כל ההודעות; DELETE /{id} - סגירת הודעה אחת ("הבנתי"); DELETE - סגירת כולן.
using BabyLink.Api.Extensions;
using BabyLink.Application.DTOs.Notifications;
using BabyLink.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabyLink.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class NotificationsController(INotificationService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<NotificationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> GetMine(CancellationToken cancellationToken)
    {
        return Ok(await service.GetMineAsync(User.GetUserId(), cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Dismiss([FromRoute] int id, CancellationToken cancellationToken)
    {
        await service.DismissAsync(id, User.GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DismissAll(CancellationToken cancellationToken)
    {
        await service.DismissAllAsync(User.GetUserId(), cancellationToken);
        return NoContent();
    }
}
