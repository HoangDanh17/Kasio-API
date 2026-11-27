using ErrorOr;
using Kasio.Application.Common.Messaging;

namespace Kasio.Application.Roles.Commands.DeleteRole;

public record class DeleteRoleCommand(Guid Id) : ICommand<Success>;
