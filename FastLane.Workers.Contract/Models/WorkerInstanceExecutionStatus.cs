namespace FastLane.Workers.Models;

public enum WorkerInstanceExecutionStatus
{
	NotStarted,
	Running,
	FinishedSuccessfully,
	Aborted,
	Failed,
	NoAssembly,
	DownloadingAssembly
}
