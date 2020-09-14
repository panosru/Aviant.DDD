namespace Aviant.DDD.Application.Commands
{
    #region

    using Core.Aggregates;
    using MediatR;

    #endregion

    public interface ICommand<out TAggregate, in TAggregateId> : IRequest<TAggregate>
        where TAggregate : class, IAggregate<TAggregateId>
        where TAggregateId : class, IAggregateId
    { }

    public interface ICommand<out TResponse> : IRequest<TResponse>
    { }

    public interface ICommand : ICommand<Unit>
    { }
}