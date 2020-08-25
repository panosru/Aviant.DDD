namespace Aviant.DDD.Infrastructure.Persistance.EventStore
{
    using System;
    using System.Threading.Tasks;
    using global::EventStore.ClientAPI;
    using Microsoft.Extensions.Logging;

    public class EventStoreConnectionWrapper : IEventStoreConnectionWrapper, IDisposable
    {
        private readonly Lazy<Task<IEventStoreConnection>> _lazyConnection;
        private readonly Uri _connectionString;
        private readonly global::Microsoft.Extensions.Logging.ILogger<EventStoreConnectionWrapper> _logger;

        public EventStoreConnectionWrapper(Uri connectionString, global::Microsoft.Extensions.Logging.ILogger<EventStoreConnectionWrapper> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
            
            _lazyConnection = new Lazy<Task<IEventStoreConnection>>(
                () =>
                {
                    return Task.Run(
                        async () =>
                        {
                            var connection = SetupConnection();

                            await connection.ConnectAsync();

                            return connection;
                        });
                });
        }
        
        // TODO: I'm not sure this is really the right approach.
        private IEventStoreConnection SetupConnection()
        {
            var settings = ConnectionSettings.Create()
                .EnableVerboseLogging()
                .UseConsoleLogger()
                .DisableTls() // https://github.com/EventStore/EventStore/issues/2547
                .Build();
            var connection = EventStoreConnection.Create(settings, _connectionString);

            connection.ErrorOccurred += async (s, e) =>
            {
                _logger.LogWarning(e.Exception,
                    $"an error has occurred on the Eventstore connection: {e.Exception.Message} . Trying to reconnect...");
                connection = SetupConnection();
                await connection.ConnectAsync();
            };
            connection.Disconnected += async (s, e) =>
            {
                _logger.LogWarning($"The Eventstore connection has dropped. Trying to reconnect...");
                connection = SetupConnection();
                await connection.ConnectAsync();
            };
            connection.Closed += async (s, e) =>
            {
                _logger.LogWarning($"The Eventstore connection was closed: {e.Reason}. Opening new connection...");
                connection = SetupConnection();
                await connection.ConnectAsync();
            };
            return connection;
        }
        
        public Task<IEventStoreConnection> GetConnectionAsync()
        {
            return _lazyConnection.Value;
        }

        public void Dispose()
        {
            if (!_lazyConnection.IsValueCreated)
                return;

            _lazyConnection.Value.Result.Dispose();
        }
    }
}