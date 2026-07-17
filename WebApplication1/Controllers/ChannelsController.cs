using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Commands;
using WebApplication1.Queries;

namespace WebApplication1.Controllers;

[ApiController]
[Route("channels")]
[Authorize]
public class ChannelsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChannelsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public record CreateChannelRequest(string Name, bool IsPrivate);
    public record AddMemberRequest(string UserId);
    public record StartDmRequest(string OtherUserId);

    [HttpGet]
    public async Task<IActionResult> GetMyChannels() =>
        Ok(await _mediator.Send(new GetMyChannelsQuery(CurrentUserId)));

    [HttpGet("public")]
    public async Task<IActionResult> GetPublicChannels() =>
        Ok(await _mediator.Send(new GetPublicChannelsQuery(CurrentUserId)));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateChannelRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Channel name is required.");
        }

        var channel = await _mediator.Send(
            new CreateChannelCommand(request.Name.Trim(), request.IsPrivate, CurrentUserId));
        return Ok(channel);
    }

    [HttpPost("{id}/join")]
    public async Task<IActionResult> Join(string id)
    {
        try
        {
            return Ok(await _mediator.Send(new JoinPublicChannelCommand(id, CurrentUserId)));
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddMember(string id, [FromBody] AddMemberRequest request)
    {
        try
        {
            await _mediator.Send(new AddMemberCommand(id, CurrentUserId, request.UserId));
            return Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPost("dm")]
    public async Task<IActionResult> StartDm([FromBody] StartDmRequest request)
    {
        try
        {
            return Ok(await _mediator.Send(new StartDmCommand(CurrentUserId, request.OtherUserId)));
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(e.Message);
        }
    }
}
