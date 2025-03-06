using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace FastLane.Workers.Contract.Exceptions
{
	public class WorkerAssemblyNotFoundException:ApplicationException
	{
		public string AssemblyLocation;
		public string AssemblyName;
		public string WorkerType;
		public object WorkerContainer;

		public WorkerAssemblyNotFoundException() : base() { }

		public WorkerAssemblyNotFoundException(string? message) : base(message) { }

		public WorkerAssemblyNotFoundException(string? message, Exception? innerException) : base(message, innerException) { }

		protected WorkerAssemblyNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}
