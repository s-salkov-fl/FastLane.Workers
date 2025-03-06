using FastLane.Workers.Contract.Services;
using FastLane.Workers.Contracts;
using System.Net.Mime;
using System.Text.Json.Serialization;

namespace FastLane.Workers.Models;

public class FileDownloadResponse : IWorkerBusMessage
{
	public string HostId { get; set; }
	public Guid Id { get; set; }
	public Guid WorkerInstanceId { get; set; }
	public string WorkerAssemblyName { get; set; }
	public byte[] FileBody { get; set; }

	[JsonIgnore]
	public string ContentEncoding { get; set; }

	[JsonIgnore]
	public string ContentType { get => MediaTypeNames.Application.Zip; set { } }

	[JsonIgnore]
	public long TimeStamp { get; set; }

	[JsonIgnore]
	public string Expiration { get; set; }
}
