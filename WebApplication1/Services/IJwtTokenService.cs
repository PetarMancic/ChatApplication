using WebApplication1.Models;

namespace WebApplication1.Services;

public interface IJwtTokenService
{
    string CreateToken(User user);
}
