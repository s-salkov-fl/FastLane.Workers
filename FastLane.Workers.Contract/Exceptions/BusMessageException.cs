using System.Runtime.Serialization;

namespace FastLane.Workers.Contract.Exceptions
{
	public class BusMessageException : ApplicationException
	{
		public BusMessageException() : base() { }

		public BusMessageException(string? message) : base(message) { }

		public BusMessageException(string? message, Exception? innerException) : base(message, innerException) { }

		protected BusMessageException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}
