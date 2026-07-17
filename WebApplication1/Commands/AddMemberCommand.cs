using MediatR;

namespace WebApplication1.Commands;

public record AddMemberCommand(string ChannelId, string RequesterUserId, string MemberUserId) : IRequest;
