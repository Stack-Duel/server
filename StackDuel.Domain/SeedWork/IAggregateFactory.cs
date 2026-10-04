namespace StackDuel.Domain.SeedWork;

public interface IAggregateFactory<TAggregate, TParams>
    where TAggregate : AggregateRoot
{
    TAggregate Create(TParams parameters);
}