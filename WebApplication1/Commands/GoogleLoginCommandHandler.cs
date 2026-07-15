using Google.Apis.Auth;
using MediatR;
using Microsoft.Extensions.Options;
using WebApplication1.Models;
using WebApplication1.Services;
using WebApplication1.Settings;

namespace WebApplication1.Commands;

public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, AuthResult>
{
    private readonly IUserStore _userStore;
    private readonly IJwtTokenService _tokenService;
    private readonly GoogleAuthSettings _googleSettings;

    public GoogleLoginCommandHandler(
        IUserStore userStore,
        IJwtTokenService tokenService,
        IOptions<GoogleAuthSettings> googleSettings)
    {
        _userStore = userStore;
        _tokenService = tokenService;
        _googleSettings = googleSettings.Value;
    }

    public async Task<AuthResult> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { _googleSettings.ClientId }
        });

        var user = await _userStore.UpsertAsync(payload.Subject, payload.Email, payload.Name);
        var token = _tokenService.CreateToken(user);

        return new AuthResult(token, user.Id, user.Name, user.Email);
    }
}
