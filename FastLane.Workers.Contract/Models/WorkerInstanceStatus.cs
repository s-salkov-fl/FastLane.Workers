namespace FastLane.Workers.Models;

public class WorkerInstanceStatus
{
	public Guid LastResponseId { get; set; }
	public string HostId { get; set; }
	public Guid InstanceId { get; set; }
	public string TypeId { get; set; }
	public WorkerInstanceExecutionStatus Status { get; set; }
	public string Input { get; set; }
	public string Result { get; set;}
}
