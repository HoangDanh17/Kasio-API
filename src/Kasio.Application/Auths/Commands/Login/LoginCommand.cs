using ErrorOr;
using Kasio.Contracts.Auth;
using MediatR;

namespace Kasio.Application.Auths.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<ErrorOr<TokenResponse>>;
