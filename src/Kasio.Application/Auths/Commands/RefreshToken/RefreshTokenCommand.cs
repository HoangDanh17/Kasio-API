using ErrorOr;
using Kasio.Contracts.Auth;
using MediatR;

namespace Kasio.Application.Auths.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<ErrorOr<TokenResponse>>;
