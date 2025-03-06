using FastLane.Workers.Contracts;
using FastLane.Workers.Configuration;
using FastLane.Workers.Models;
using FastLane.Workers.Contract.Services;
using Microsoft.Extensions.Options;

namespace FastLane.Workers.Server.Consumers;

public class RestartWorkerResponseConsumer : WorkerBusConsumer<RestartWorkerResponse>
{
	private readonly ILogger<RestartWorkerResponseConsumer> _logger;
	private readonly RestartWorkersState restartWorkersState;

	public RestartWorkerResponseConsumer(ILogger<RestartWorkerResponseConsumer> logger,
		RestartWorkersState restartWorkersState,
		IOptions<BusConfiguration> busConfiguration) : base(busConfiguration, logger)
	{
		_logger = logger;
		this.restartWorkersState = restartWorkersState;
	}

	public override Task Consume(IWorkerBus bus, RestartWorkerResponse message, CancellationToken cancelToken)
	{
		restartWorkersState.RestartWorkerStates.RemoveAll(w => w.WorkerInstanceId == message.WorkerInstanceId && w.HostId == message.HostId);

		restartWorkersState.RestartWorkerStates.Add(
			new RestartWorkerStatus
			{
				HostId = message.HostId,
				LastResponseId = message.Id,
				Processed = message.Processed,
				WorkerInstanceId = message.WorkerInstanceId
			});

		return Task.CompletedTask;
	}
}
