using ErrorOr;
using Kasio.Application.Common.Messaging;

namespace Kasio.Application.Users.Commands.DeactivateUser;

/// <summary>Khóa tài khoản (soft delete) — không xóa dữ liệu, mất hiệu lực ngay.</summary>
public sealed record DeactivateUserCommand(Guid UserId, Guid ActorUserId) : ICommand<Success>;
