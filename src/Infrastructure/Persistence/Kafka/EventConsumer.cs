namespace Aviant.DDD.Infrastructure.Persistence.Kafka
{
    #region

    using System;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Confluent.Kafka;
    using Core.Aggregates;
    using Core.EventBus;
    using Core.Events;
    using Core.Services;
    using Microsoft.Extensions.Logging;

    #endregion

    public class EventConsumer<TAggregate, TAggregateId, TDeserializer>
        : IDisposable, IEventConsumer<TAggregate, TAggregateId, TDeserializer>
        where TAggregate : IAggregate<TAggregateId>
        where TAggregateId : class, IAggregateId
        where TDeserializer : class, IDeserializer<TAggregateId>, new()
    {
        #region Delegates

        public delegate void ConsumerStoppedHandler(object sender);

        public delegate void ExceptionThrownHandler(object sender, Exception e);

        #endregion

        private readonly IEventDeserializer _eventDeserializer;

        private readonly ILogger<EventConsumer<TAggregate, TAggregateId, TDeserializer>> _logger;

        private IConsumer<TAggregateId, string> _eventConsumer;

        public EventConsumer(
            IEventDeserializer                                              eventDeserializer,
            EventConsumerConfig                                             config,
            ILogger<EventConsumer<TAggregate, TAggregateId, TDeserializer>> logger)
        {
            _eventDeserializer = eventDeserializer;
            _logger            = logger;

            var aggregateType = typeof(TAggregate);

            var consumerConfig = new ConsumerConfig
            {
                GroupId            = config.ConsumerGroup,
                BootstrapServers   = config.KafkaConnectionString,
                AutoOffsetReset    = AutoOffsetReset.Earliest,
                EnablePartitionEof = true
            };

            var consumerBuilder        = new ConsumerBuilder<TAggregateId, string>(consumerConfig);
            var keyDeserializerFactory = new KeyDeserializerFactory();
            consumerBuilder.SetKeyDeserializer(keyDeserializerFactory.Create<TDeserializer, TAggregateId>());

            _eventConsumer = consumerBuilder.Build();

            var topicName = $"{config.TopicBaseName}-{aggregateType.Name}";
            _eventConsumer.Subscribe(topicName);
        }

        #region IDisposable Members

        public void Dispose()
        {
            _eventConsumer?.Dispose();
            _eventConsumer = null;
        }

        #endregion

        #region IEventConsumer<TAggregate,TAggregateId,TDeserializer> Members

        public Task ConsumeAsync(CancellationToken stoppingToken)
        {
            return Task.Run(
                async () =>
                {
                    var topics = string.Join(",", _eventConsumer.Subscription);

                    _logger.LogInformation(
                        "started Kafka consumer {ConsumerName} on {ConsumerTopic}",
                        _eventConsumer.Name,
                        topics);

                    while (!stoppingToken.IsCancellationRequested)
                        try
                        {
                            ConsumeResult<TAggregateId, string> cr = _eventConsumer.Consume(stoppingToken);

                            if (cr.IsPartitionEOF)
                                continue;

                            var messageTypeHeader = cr.Message.Headers.First(h => h.Key == "type");
                            var eventType         = Encoding.UTF8.GetString(messageTypeHeader.GetValueBytes());

                            IEvent<TAggregateId> @event =
                                _eventDeserializer.Deserialize<TAggregateId>(eventType, cr.Message.Value);

                            if (null == @event)
                                throw new SerializationException(
                                    $"unable to deserialize notification {eventType} : {cr.Message.Value}");

                            await OnEventReceived(@event);
                        }
                        catch (OperationCanceledException ex)
                        {
                            _logger.LogWarning(
                                ex,
                                "consumer {ConsumerName} on {ConsumerTopic} was stopped: {StopReason}",
                                _eventConsumer.Name,
                                topics,
                                ex.Message);
                            OnConsumerStopped();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"an exception has occurred while consuming a message: {ex.Message}");
                            OnExceptionThrown(ex);
                        }
                },
                stoppingToken);
        }

        public event EventReceivedHandler<TAggregateId> EventReceived;

        #endregion

        protected virtual Task OnEventReceived(IEvent<TAggregateId> e)
        {
            EventReceivedHandler<TAggregateId> handler = EventReceived;

            return handler?.Invoke(this, e);
        }

        public event ExceptionThrownHandler ExceptionThrown;

        protected virtual void OnExceptionThrown(Exception e)
        {
            var handler = ExceptionThrown;
            handler?.Invoke(this, e);
        }

        public event ConsumerStoppedHandler ConsumerStopped;

        protected virtual void OnConsumerStopped()
        {
            var handler = ConsumerStopped;
            handler?.Invoke(this);
        }
    }
}