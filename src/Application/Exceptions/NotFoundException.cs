namespace Aviant.DDD.Application.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class NotFoundException : ApplicationException
    {
        public NotFoundException(string message)
            : base(message)
        { }

        public NotFoundException(string message, Exception innerInner)
            : base(message, innerInner)
        { }

        public NotFoundException(string name, object key)
            : base($"Entity \"{name}\" ({key}) was not found.")
        { }

        protected NotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}