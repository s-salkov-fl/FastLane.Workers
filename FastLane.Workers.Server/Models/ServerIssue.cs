using System.Text.Json.Serialization;

namespace FastLane.Workers.Models;

[JsonDerivedType(typeof(ServerHostConflictIssue))]
public class ServerIssue
{
	protected string description = "Unknown Issue";
	public string Description { get => description; set => description = value; }
	public bool IsCritical { get; set; }
	public ServerIssue() { }
	public ServerIssue(string description, bool isCritical)
	{
		this.description = description;
		IsCritical = isCritical;
	}
}
