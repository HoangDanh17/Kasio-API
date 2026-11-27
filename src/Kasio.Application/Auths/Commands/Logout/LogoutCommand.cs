using ErrorOr;
using MediatR;

namespace Kasio.Application.Auths.Commands.Logout;

public sealed record LogoutCommand(string RefreshToken) : IRequest<ErrorOr<Success>>;
