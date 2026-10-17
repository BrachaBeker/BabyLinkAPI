// POST /api/users - הרשמה (ללא [Authorize]). 201, או 409 אם האימייל קיים.
// (שם משאב ולא "register" - לפי דרישת REST.)
using BabyLink.Application.DTOs.Auth;
using BabyLink.Application.DTOs.Users;
using BabyLink.Application.Interfaces.Services;
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
        return Created($"/api/users/{user.Id}", user);
    }
}
