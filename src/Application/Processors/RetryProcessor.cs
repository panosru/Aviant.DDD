namespace Aviant.DDD.Application.Processors
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Services;
    using MediatR;
    using Polly;

    public sealed class RetryProcessor<TNotification>
        : INotificationHandler<TNotification>
        where TNotification : INotification
    {
        private readonly INotificationHandler<TNotification> _inner;

        private readonly IAsyncPolicy? _retryPolicy;

        public RetryProcessor(INotificationHandler<TNotification> inner)
        {
            _inner = inner;

            if (_inner is IRetry handler)
                _retryPolicy = handler.RetryPolicy();
        }

        public Task Handle(TNotification notification, CancellationToken cancellationToken)
        {
            return _retryPolicy?.ExecuteAsync(
                       () =>
                           _inner.Handle(notification, cancellationToken))
                ?? throw new NullReferenceException(nameof(_retryPolicy));
        }
    }
}