// /api/service-requests - כל ה-endpoints דורשים התחברות; יצירה/עריכה/מחיקה רק להורה, תפיסה רק לשמרטפית.
// ה-Controller דק: שולף את המשתמש מה-JWT, קורא ל-Service ומחזיר סטטוס. אין כאן לוגיקה עסקית.
// הורה רואה רק את הבקשות שלו (ברשימה ובפרטים) - נאכף ב-Service.
using BabyLink.Api.Extensions;
using BabyLink.Application.DTOs.Common;
using BabyLink.Application.DTOs.ServiceRequests;
using BabyLink.Application.Interfaces.Services;
using BabyLink.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabyLink.Api.Controllers;

[ApiController]
[Route("api/service-requests")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class ServiceRequestsController(IServiceRequestService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<ServiceRequestListItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ServiceRequestListItemDto>>> GetAll(
        [FromQuery] ServiceRequestQueryParameters query, CancellationToken cancellationToken)
    {
        return Ok(await service.GetPagedAsync(query, User.GetCurrentUser(), cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ServiceRequestDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceRequestDetailsDto>> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        return Ok(await service.GetByIdAsync(id, User.GetCurrentUser(), cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Parent))]
    [ProducesResponseType<ServiceRequestDetailsDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ServiceRequestDetailsDto>> Create(
        [FromBody] CreateServiceRequestDto dto, CancellationToken cancellationToken)
    {
        var created = await service.CreateAsync(dto, User.GetUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Parent))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        [FromRoute] int id, [FromBody] UpdateServiceRequestDto dto, CancellationToken cancellationToken)
    {
        await service.UpdateAsync(id, dto, User.GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Parent))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, User.GetUserId(), cancellationToken);
        return NoContent();
    }

    // תפיסת בקשה ע"י שמרטפית. 409 = מישהי אחרת הקדימה (או שיבוץ חופף).
    [HttpPut("{id:int}/assignment")]
    [Authorize(Roles = nameof(UserRole.Babysitter))]
    [ProducesResponseType<ServiceRequestDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServiceRequestDetailsDto>> Assign([FromRoute] int id, CancellationToken cancellationToken)
    {
        return Ok(await service.AssignAsync(id, User.GetUserId(), cancellationToken));
    }

    // ביטול השיבוץ ע"י השמרטפית שתפסה ("מחיקת השיבוץ"). 400 = פחות מ-24 שעות לפני המועד.
    // 409 = הבקשה כבר לא משובצת, או שמישהו שינה אותה בדיוק עכשיו.
    [HttpDelete("{id:int}/assignment")]
    [Authorize(Roles = nameof(UserRole.Babysitter))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Unassign([FromRoute] int id, CancellationToken cancellationToken)
    {
        await service.UnassignAsync(id, User.GetUserId(), cancellationToken);
        return NoContent();
    }
}
