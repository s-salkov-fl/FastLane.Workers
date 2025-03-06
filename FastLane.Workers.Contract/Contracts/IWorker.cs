using System.Text.Json;

namespace FastLane.Workers.Contracts
{
	public interface IWorker
	{
		Task<JsonElement?> ExecuteAsync(JsonElement? input, CancellationToken cancellationToken);
	}
}