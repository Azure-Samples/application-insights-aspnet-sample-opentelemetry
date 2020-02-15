using System;
using System.Runtime.Serialization;

namespace Sample.RabbitMQProcessor
{
    [Serializable]
    public class InvalidEventNameException : Exception
    {
        public InvalidEventNameException()
        {
        }

        public InvalidEventNameException(string message) : base(message)
        {
        }

        public InvalidEventNameException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidEventNameException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}