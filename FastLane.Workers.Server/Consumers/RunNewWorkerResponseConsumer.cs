using FastLane.Workers.Configuration;
using FastLane.Workers.Contract.Services;
using FastLane.Workers.Contracts;
using FastLane.Workers.Models;
using Microsoft.Extensions.Options;

namespace FastLane.Workers.Server.Consumers;

public class RunNewWorkerResponseConsumer : WorkerBusConsumer<RunNewWorkerResponse>
{
	private readonly ILogger<RunNewWorkerResponseConsumer> _logger;
	private readonly RunNewWorkersState runNewWorkersState;

	public RunNewWorkerResponseConsumer(ILogger<RunNewWorkerResponseConsumer> logger,
		RunNewWorkersState runNewWorkersState,
		IOptions<BusConfiguration> busConfiguration) : base(busConfiguration, logger)
	{
		_logger = logger;
		this.runNewWorkersState = runNewWorkersState;
	}

	public override Task Consume(IWorkerBus bus, RunNewWorkerResponse message, CancellationToken cancelToken)
	{
		var addedState = runNewWorkersState.RunWorkerStates.TryAdd(message.Id,
			new RunNewWorkerStatus
			{
				HostId = message.HostId,
				LastResponseId = message.Id,
				Processed = message.Processed,
				WorkerInstanceId = message.WorkerInstanceId,
				NeedAssembly = message.NeedAssembly,
				AssemblyLocation = message.AssemblyLocation,
				AssemblyName = message.AssemblyName,
				WorkerType = message.WorkerType
			});

		if (!addedState) throw new InvalidOperationException("Unable to store Ran Worker Response in the collection");

		return Task.CompletedTask;
	}
}
