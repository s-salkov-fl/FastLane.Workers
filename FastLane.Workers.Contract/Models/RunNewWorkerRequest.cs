using FastLane.Workers.Contract.Services;
using FastLane.Workers.Contracts;
using System.Text.Json.Serialization;

namespace FastLane.Workers.Models
{
    public class RunNewWorkerRequest : IWorkerBusMessage
    {
        public Guid Id { get; set; }
        public string HostId { get; set; }
        public string WorkerTypeName { get; set; }
        public string jsonInput { get; set; }

		[JsonIgnore]
		public string ContentEncoding { get; set; }

		[JsonIgnore]
		public string ContentType { get; set; }

		[JsonIgnore]
		public long TimeStamp { get; set; }

		[JsonIgnore]
		public string Expiration { get; set; }
	}
}
