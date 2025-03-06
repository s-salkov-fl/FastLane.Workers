using FastLane.Workers.Contract.Services;
using FastLane.Workers.Contracts;
using FastLane.Workers.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FastLane.Workers.Models;

public class HostWorkerInstanceStatusResponse : IWorkerBusMessage
{
    public Guid Id { get; set; }
    public string HostId { get; set; }
    public Guid InstanceId { get; set; }
    public string TypeId { get; set; }
    public WorkerInstanceExecutionStatus Status { get; set; }
    public string Input { get; set; }
    public string Result { get; set; }

	[JsonIgnore]
	public string ContentEncoding { get; set; }

	[JsonIgnore]
	public string ContentType { get; set; }

	[JsonIgnore]
	public long TimeStamp { get; set; }

	[JsonIgnore]
	public string Expiration { get; set; }
}
