// POST /api/tokens - התחברות (ללא [Authorize]): "יצירת token" כמשאב, במקום "/login". 200 + JWT, או 401.
using BabyLink.Application.DTOs.Auth;
using BabyLink.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace BabyLink.Api.Controllers;

[ApiController]
[Route("api/tokens")]
[Produces("application/json")]
public class TokensController(IAuthService authService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Create([FromBody] LoginRequestDto dto, CancellationToken cancellationToken)
    {
        return Ok(await authService.LoginAsync(dto, cancellationToken));
    }
}
