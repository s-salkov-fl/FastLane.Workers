namespace FastLane.Workers.Models;

public class StopWorkerStatus
{
	public Guid LastResponseId { get; set; }
	public string HostId { get; set; }
	public Guid WorkerInstanceId { get; set; }
	public bool Processed { get; set; }
}
