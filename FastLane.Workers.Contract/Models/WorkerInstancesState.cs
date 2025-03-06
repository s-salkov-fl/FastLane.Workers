namespace FastLane.Workers.Models;

public class WorkerInstancesState
{
	public List<WorkerInstanceStatus> WorkerInstanceStatuses { get; set; } = new List<WorkerInstanceStatus>();
}
