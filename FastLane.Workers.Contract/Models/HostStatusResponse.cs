using FastLane.Workers.Contract.Services;
using FastLane.Workers.Contracts;
using System.Text.Json.Serialization;

namespace FastLane.Workers.Models;

public class HostStatusResponse : IWorkerBusMessage
{
    public Guid Id { get; set; }
    public string HostId { get; set; }
    public string HostDnsName { get; set; }
    public string HostProcessId { get; set; }
    public DateTimeOffset HostStartDate { get; set; }
    public int NumberWorkersRun { get; set; }
    public HostServiceHealthStatus HealthStatus { get; set; }
    public IEnumerable<WorkerInstanceBriefInfo> WorkerInstances { get; set; }

	[JsonIgnore]
	public string ContentEncoding { get; set; }

	[JsonIgnore]
	public string ContentType { get; set; }

	[JsonIgnore]
	public long TimeStamp { get; set; }

	[JsonIgnore]
	public string Expiration { get; set; }
}
