using MediatR;

namespace WebApplication1.Queries;

public record IsChannelMemberQuery(string ChannelId, string UserId) : IRequest<bool>;
