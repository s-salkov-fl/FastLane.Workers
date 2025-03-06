using FastLane.Workers.Contracts;
using System.Text.Json;

namespace FastLane.Workers.Implementations;

public class WorkerLong : IWorker
{
	public Task<JsonElement?> ExecuteAsync(JsonElement? input, CancellationToken cancellationToken)
	{
		int currentVal = 1;

		while(currentVal < 5000)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Task.Delay(1000, cancellationToken).Wait();
			currentVal += 3;
		}

		return Task.FromResult((JsonElement?)JsonSerializer.SerializeToElement(currentVal));
	}
}