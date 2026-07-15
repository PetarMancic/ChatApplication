using MediatR;
using WebApplication1.Models;

namespace WebApplication1.Commands;

public record GoogleLoginCommand(string IdToken) : IRequest<AuthResult>;
