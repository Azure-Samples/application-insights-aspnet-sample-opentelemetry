using System;
using System.Runtime.Serialization;

namespace Sample.Common
{
    [Serializable]
    internal class GeneratedFailureException : Exception
    {
        public GeneratedFailureException()
        {
        }

        public GeneratedFailureException(string message) : base(message)
        {
        }

        public GeneratedFailureException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected GeneratedFailureException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}