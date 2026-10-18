// POST /api/users - הרשמה (ללא [Authorize]): 201 + Location, או 409 אם האימייל קיים.
// GET /api/users/{id} - פרטי המשתמש המחובר בלבד (המשאב שה-Location של ההרשמה מצביע עליו).
// PUT /api/users/{id} - עדכון הפרופיל של המשתמש המחובר בלבד (שם, טלפון, עיר וכתובת).
using BabyLink.Api.Extensions;
using BabyLink.Application.DTOs.Auth;
using BabyLink.Application.DTOs.Users;
using BabyLink.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabyLink.Api.Controllers;

[ApiController]
[Route("api/users")]
[Produces("application/json")]
public class UsersController(IAuthService authService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequestDto dto, CancellationToken cancellationToken)
    {
        var user = await authService.RegisterAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        return Ok(await authService.GetUserAsync(id, User.GetUserId(), cancellationToken));
    }

    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> Update([FromRoute] int id, [FromBody] UpdateUserDto dto, CancellationToken cancellationToken)
    {
        return Ok(await authService.UpdateUserAsync(id, User.GetUserId(), dto, cancellationToken));
    }
}
