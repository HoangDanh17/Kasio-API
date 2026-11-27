using ErrorOr;
using MediatR;

namespace Kasio.Application.Common.Messaging;

public interface IQuery<TResponse> : IRequest<ErrorOr<TResponse>> { }
