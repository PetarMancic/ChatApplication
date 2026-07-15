using Google.Apis.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Commands;

namespace WebApplication1.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public record GoogleLoginRequest(string IdToken);

    [HttpPost("google")]
    public async Task<IActionResult> Google([FromBody] GoogleLoginRequest request)
    {
        try
        {
            var result = await _mediator.Send(new GoogleLoginCommand(request.IdToken));
            return Ok(result);
        }
        catch (InvalidJwtException)
        {
            return Unauthorized();
        }
    }
}
