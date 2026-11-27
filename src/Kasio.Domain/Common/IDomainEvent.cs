namespace Kasio.Domain.Common;

public interface IDomainEvent
{
    string EventType { get; }
    DateTime OccurredOnUtc { get; }
}
