using FastLane.Workers.Contracts;
using System.Text.Json;

namespace FastLane.Workers.Implementations;

public class WorkerBuggy : IWorker
{
	public Task<JsonElement?> ExecuteAsync(JsonElement? input, CancellationToken cancellationToken)
	{
		(int startVal, int endVal, int step) intervalCalc = (0, 100, 1);


		int currentVal = intervalCalc.startVal;

		while(currentVal < intervalCalc.endVal)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Task.Delay(1000, cancellationToken).Wait();
			currentVal += intervalCalc.step;
			if (currentVal == 66) throw new ApplicationException("Bad number in process");
		}

		return Task.FromResult((JsonElement?)JsonSerializer.SerializeToElement(currentVal));
	}
}