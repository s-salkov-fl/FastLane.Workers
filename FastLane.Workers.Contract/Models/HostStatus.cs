namespace FastLane.Workers.Models;

public class HostStatus
{
	public Guid LastResponseId { get; set; }
	public string HostId { get; set; }
	public string HostDnsName { get; set; }
	public string HostProcessId { get; set; }
	public DateTimeOffset HostStartDate { get; set; }
	public DateTimeOffset HostObtainStatusDate { get; set; }
	public int NumberWorkersRun { get; set; }
	public HostServiceHealthStatus HostHealth { get; set; }
	public IEnumerable<WorkerInstanceBriefInfo> WorkerInstances { get; set; }

	public override bool Equals(object? obj)
	{
		var hostStat = obj as HostStatus;
		if (hostStat != null)
		{
			
			return hostStat.HostId == HostId && 
				hostStat.HostDnsName == HostDnsName &&
				hostStat.HostProcessId == HostProcessId;
		}
		return base.Equals(obj);
	}

	public override int GetHashCode()
	{
		return HostId.GetHashCode() ^ HostDnsName.GetHashCode() ^ HostProcessId.GetHashCode();
	}
}
