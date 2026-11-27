using DotNetCore.CAP;
using Kasio.Application.Common.Interfaces;

namespace Kasio.Infrastructure.Persistence.Messaging;

public class CapEventPublisher(ICapPublisher capPublisher) : IEventPublisher
{
    public async Task PublishAsync(string topic, object? content, CancellationToken cancellationToken = default)
    {
        await capPublisher.PublishAsync(topic, content, cancellationToken: cancellationToken);
    }
}
