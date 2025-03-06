using Microsoft.Extensions.Options;
using FastLane.Workers.Contracts;
using FastLane.Workers.Configuration;
using FastLane.Workers.Models;
using FastLane.Workers.Contract.Services;

namespace FastLane.Workers.Server.Consumers;

public class StopWorkerResponseConsumer : WorkerBusConsumer<StopWorkerResponse>
{
	private readonly ILogger<StopWorkerResponseConsumer> _logger;
	private readonly StopWorkersState stopWorkersState;

	public StopWorkerResponseConsumer(ILogger<StopWorkerResponseConsumer> logger,
		StopWorkersState stopWorkersState,
		IOptions<BusConfiguration> busConfiguration) : base(busConfiguration, logger)
	{
		_logger = logger;
		this.stopWorkersState = stopWorkersState;
	}

	public override Task Consume(IWorkerBus bus, StopWorkerResponse message, CancellationToken cancelToken)
	{
		stopWorkersState.StopWorkerStates.RemoveAll(w => w.WorkerInstanceId == message.WorkerInstanceId && w.HostId == message.HostId);

		stopWorkersState.StopWorkerStates.Add(
			new StopWorkerStatus
			{
				HostId = message.HostId,
				LastResponseId = message.Id,
				Processed = message.Processed,
				WorkerInstanceId = message.WorkerInstanceId
			});

		return Task.CompletedTask;
	}
}
