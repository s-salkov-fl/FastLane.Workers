using System.Collections.Concurrent;

namespace FastLane.Workers.Models;

public class RunNewWorkersState
{
	public ConcurrentDictionary<Guid, RunNewWorkerStatus> RunWorkerStates { get; set; } = new ConcurrentDictionary<Guid, RunNewWorkerStatus>();
}
