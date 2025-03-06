using FastLane.Workers.Bucket.Services;
using FastLane.Workers.Configuration;
using FastLane.Workers.Contract.Exceptions;
using FastLane.Workers.Contract.Services;
using FastLane.Workers.Models;
using Microsoft.Extensions.Options;

namespace FastLane.Workers.Bucket.Consumers;

public class RunNewWorkerRequestConsumer : WorkerBusConsumer<RunNewWorkerRequest>
{
	private readonly ILogger<RunNewWorkerRequestConsumer> _logger;
	private readonly WorkersDispatcher workersDispatcher;

	public RunNewWorkerRequestConsumer(ILogger<RunNewWorkerRequestConsumer> logger,
		IOptions<BusConfiguration> busConfiguration,
		WorkersDispatcher workersDispatcher) : base(busConfiguration, logger)
	{
		_logger = logger;
		this.workersDispatcher = workersDispatcher;
	}

	public override async Task Consume(IWorkerBus bus, RunNewWorkerRequest message, CancellationToken cancelToken)
	{
		WorkerContainer containerWorker = null;
		var response = new RunNewWorkerResponse
		{
			Id = message.Id,
			HostId = message.HostId,
			Processed = true
		};

		try
		{
			containerWorker = await workersDispatcher.RunWorkerInstance(message.WorkerTypeName, message.jsonInput, cancelToken);
		}
		catch (WorkerAssemblyNotFoundException ex)
		{
			response.WorkerType = message.WorkerTypeName;
			response.NeedAssembly = true;
			response.AssemblyLocation = ex.AssemblyLocation;
			response.AssemblyName = ex.AssemblyName;
			containerWorker = (WorkerContainer)ex.WorkerContainer;
			containerWorker.Status = WorkerInstanceExecutionStatus.NoAssembly;
		}

		response.WorkerInstanceId = (containerWorker == null) ? Guid.Empty : containerWorker.WorkerInstanceId;
		response.Processed = containerWorker != null;

		await bus.PublishFromHostToServer(response);
	}
}
