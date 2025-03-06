using FastLane.Workers.Contracts;
using System.Text.Json;

namespace FastLaneFastLane.Workers.Implementations;

public class WorkerCalculator : IWorker
{
	class ConfigCalc 
	{ 
		public int startVal { get; set; } = 0;
		public int endVal { get; set; } = 100;
		public int step { get; set; } = 1;
	}
	public Task<JsonElement?> ExecuteAsync(JsonElement? input, CancellationToken cancellationToken)
	{
		ConfigCalc intervalCalc = null;

		if (null != input)
			intervalCalc = JsonSerializer.Deserialize<ConfigCalc>(input.Value);

		intervalCalc = intervalCalc ?? new ConfigCalc();

		int currentVal = intervalCalc.startVal;

		while(currentVal < intervalCalc.endVal)
		{
			cancellationToken.ThrowIfCancellationRequested();
			currentVal += intervalCalc.step;
		}

		return Task.FromResult((JsonElement?)JsonSerializer.SerializeToElement(currentVal));
	}
}