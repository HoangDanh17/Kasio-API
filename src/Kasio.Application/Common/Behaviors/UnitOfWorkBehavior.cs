using ErrorOr;
using Kasio.Application.Common.Interfaces;
using Kasio.Application.Common.Messaging;
using MediatR;

namespace Kasio.Application.Common.Behaviors;

/// <summary>
/// Bọc transaction cho command. Không ràng buộc generic theo ICommand&lt;TResponse&gt; vì
/// MediatR đóng open behavior bằng response type thật (ErrorOr&lt;X&gt;), nên ràng buộc đó
/// không bao giờ thoả và behavior sẽ bị DI bỏ qua. Dùng marker <see cref="ICommandBase"/>.
/// </summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICommandBase)
            return await next(cancellationToken);

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await next(cancellationToken);

            if (result.IsError)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return result;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return result;
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
