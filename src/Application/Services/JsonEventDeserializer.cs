namespace Aviant.DDD.Application.Services
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Core.Aggregates;
    using Core.Events;
    using Core.Services;
    using Newtonsoft.Json;

    #endregion

    public class JsonEventDeserializer : IEventDeserializer
    {
        private readonly IEnumerable<Assembly> _assemblies;

        public JsonEventDeserializer(IEnumerable<Assembly>? assemblies)
        {
            _assemblies = assemblies ?? new[] { Assembly.GetExecutingAssembly() };
        }

        #region IEventDeserializer Members

        public IEvent<TAggregateId> Deserialize<TAggregateId>(string type, byte[] data)
            where TAggregateId : IAggregateId
        {
            var jsonData = Encoding.UTF8.GetString(data);

            return Deserialize<TAggregateId>(type, jsonData);
        }

        public IEvent<TAggregateId> Deserialize<TAggregateId>(string type, string data)
            where TAggregateId : IAggregateId
        {
            //TODO: cache types
            var eventType = _assemblies
                               .Select(a => a.GetType(type, false))
                               .FirstOrDefault(t => t != null)
                         ?? Type.GetType(type);

            if (null == eventType)
                throw new ArgumentOutOfRangeException(nameof(type), $"invalid notification type: {type}");

            // as of 01/10/2020, "Deserialization to reference types without a parameterless constructor isn't supported."
            // https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-how-to
            // apparently it's being worked on: https://github.com/dotnet/runtime/issues/29895

            var result = JsonConvert.DeserializeObject(
                data,
                eventType,
                new JsonSerializerSettings
                {
                    ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                    ContractResolver    = new PrivateSetterContractResolver()
                });

            return (IEvent<TAggregateId>) result;
        }

        #endregion
    }
}