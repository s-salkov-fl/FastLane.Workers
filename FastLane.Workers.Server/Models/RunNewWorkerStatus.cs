namespace FastLane.Workers.Models;

public class RunNewWorkerStatus
{
	public Guid LastResponseId { get; set; }
	public string HostId { get; set; }
	public Guid WorkerInstanceId { get; set; }
	public bool Processed { get; set; }
	public bool NeedAssembly { get; set; }
	public string AssemblyLocation { get; set; }
	public string AssemblyName { get; set; }
	public string WorkerType { get; set; }
}
