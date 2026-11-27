namespace Kasio.Domain.Common;

public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    protected void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    void IAggregateRoot.ClearDomainEvents()
    {
        ClearDomainEvents();
    }
}
