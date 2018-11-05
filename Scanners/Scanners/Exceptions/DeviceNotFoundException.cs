using System;
using System.Runtime.Serialization;

namespace Scanners.Exceptions
{
    /// <summary>
    ///		Represents error that occur when fingerprint scanning device is not found.
    /// </summary>
    /// <seealso cref="System.Exception" />
    [Serializable]
    internal class NullReferenceException : Exception
    {
        /// <summary>
        ///		Initializes a new instance of the <see cref="NullReferenceException"/> class.
        /// </summary>
        /// 
        public NullReferenceException()
        { }

        /// <summary>
        ///		Initializes a new instance of the <see cref="NullReferenceException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// 
        public NullReferenceException(string message) : base(message)
        { }

        /// <summary>
        ///		Initializes a new instance of the <see cref="NullReferenceException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (<see langword="Nothing" /> in Visual Basic) if no inner exception is specified.</param>
        /// 
        public NullReferenceException(string message, Exception innerException) : base(message, innerException)
        { }

        /// <summary>
        ///		Initializes a new instance of the <see cref="NullReferenceException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        /// 
        protected NullReferenceException(SerializationInfo info, StreamingContext context) : base(info, context)
        { }
    }
}