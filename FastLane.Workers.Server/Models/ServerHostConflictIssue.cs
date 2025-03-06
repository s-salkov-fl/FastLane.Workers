namespace FastLane.Workers.Models;

public class ServerHostConflictIssue: ServerIssue
{
	public ServerHostConflictIssue() : base("Following hosts sharing same HostId", true) { }
	public ServerHostConflictIssue(string description) : base(description, true) { }
	public Dictionary<string, List<HostStatus>> HostIdStateMatches { get; set; } = new Dictionary<string, List<HostStatus>>();
}
