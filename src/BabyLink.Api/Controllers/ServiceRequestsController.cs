// /api/service-requests - כל ה-endpoints דורשים התחברות; יצירה/עריכה/מחיקה רק להורה.
// ה-Controller דק: שולף UserId מה-JWT, קורא ל-Service ומחזיר סטטוס. אין כאן לוגיקה עסקית.
// (PUT /{id}/assignment - תפיסת בקשה - יתווסף בשלב 8.)
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
        return Ok(await service.GetPagedAsync(query, User.GetUserId(), cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ServiceRequestDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceRequestDetailsDto>> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        return Ok(await service.GetByIdAsync(id, cancellationToken));
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
}
