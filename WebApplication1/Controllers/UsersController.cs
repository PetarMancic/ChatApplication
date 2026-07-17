using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Queries;

namespace WebApplication1.Controllers;

[ApiController]
[Route("users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q = "")
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Ok(await _mediator.Send(new SearchUsersQuery(q, currentUserId)));
    }
}
