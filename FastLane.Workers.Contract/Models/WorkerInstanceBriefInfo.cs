namespace FastLane.Workers.Models;

public class WorkerInstanceBriefInfo
{
	public Guid InstanceId { get; set; }
	public string TypeId { get; set; }
	public WorkerInstanceExecutionStatus Status { get; set; }
}
