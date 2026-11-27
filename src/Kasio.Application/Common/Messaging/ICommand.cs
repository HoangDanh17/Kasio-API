using ErrorOr;
using MediatR;

namespace Kasio.Application.Common.Messaging;

public interface ICommand<TResponse> : IRequest<ErrorOr<TResponse>>, ICommandBase { }
