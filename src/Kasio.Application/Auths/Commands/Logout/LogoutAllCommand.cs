using ErrorOr;
using MediatR;

namespace Kasio.Application.Auths.Commands.Logout;

public sealed record LogoutAllCommand(Guid UserId) : IRequest<ErrorOr<Success>>;
