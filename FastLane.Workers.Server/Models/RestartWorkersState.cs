namespace FastLane.Workers.Models;

public class RestartWorkersState
{
	public List<RestartWorkerStatus> RestartWorkerStates { get; set; } = new List<RestartWorkerStatus>();
}
