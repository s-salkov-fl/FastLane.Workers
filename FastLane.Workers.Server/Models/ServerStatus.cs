namespace FastLane.Workers.Models;

public class ServerStatus
{
	public HostsState HostsState { get; set; }
	public List<ServerIssue> Issues { get; set; } = new List<ServerIssue>();
	public string Status { get; set; }
	public ServerStatus(HostsState hostsState)
	{
		HostsState = hostsState;
	}

	public IEnumerable<ServerHostConflictIssue> FindCriticalProblemHost(string hostId)
	{
		return Issues.FindAll(p =>
		{
			var hostIssue = p as ServerHostConflictIssue;
			if (hostIssue != null && p.IsCritical && hostIssue.HostIdStateMatches.ContainsKey(hostId))
			{
				return true;
			}
			return false;
		}).Select(h => ((ServerHostConflictIssue)h));

	}
}
