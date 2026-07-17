using MediatR;
using WebApplication1.Models;

namespace WebApplication1.Queries;

public record SearchUsersQuery(string Query, string ExcludeUserId) : IRequest<IReadOnlyList<UserSummary>>;
