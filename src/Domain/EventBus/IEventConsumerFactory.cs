namespace Aviant.DDD.Domain.EventBus
{
    using Aggregates;

    public interface IEventConsumerFactory
    {
        IEventConsumer Build<TAggregate, TAggregateId, TDeserializer>()
            where TAggregate : IAggregate<TAggregateId>
            where TAggregateId : IAggregateId;
    }
}