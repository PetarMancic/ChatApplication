using MediatR;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Queries;

public class SearchUsersQueryHandler : IRequestHandler<SearchUsersQuery, IReadOnlyList<UserSummary>>
{
    private readonly IUserStore _userStore;

    public SearchUsersQueryHandler(IUserStore userStore)
    {
        _userStore = userStore;
    }

    public async Task<IReadOnlyList<UserSummary>> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return Array.Empty<UserSummary>();
        }

        var users = await _userStore.SearchAsync(request.Query, request.ExcludeUserId);
        return users.Select(u => new UserSummary(u.Id, u.Name, u.Email)).ToList();
    }
}
