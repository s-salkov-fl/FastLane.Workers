using FastLane.Workers.Bucket.Services;
using FastLane.Workers.Configuration;
using FastLane.Workers.Contract.Services;
using FastLane.Workers.Contracts;
using FastLane.Workers.Models;
using Microsoft.Extensions.Options;

namespace FastLane.Workers.Bucket.Consumers;

public class RestartWorkerRequestConsumer : WorkerBusConsumer<RestartWorkerRequest>
{
	private readonly ILogger<RestartWorkerRequestConsumer> _logger;
	private readonly WorkersDispatcher workersDispatcher;

	public RestartWorkerRequestConsumer(ILogger<RestartWorkerRequestConsumer> logger,
		IOptions<BusConfiguration> busConfiguration,
		WorkersDispatcher workersDispatcher) : base(busConfiguration, logger)
	{
		_logger = logger;
		this.workersDispatcher = workersDispatcher;
	}

	public override async Task Consume(IWorkerBus bus, RestartWorkerRequest message, CancellationToken cancelToken)
	{
		WorkerContainer workContainer = workersDispatcher.workersInfo.FirstOrDefault(w => w.WorkerInstanceId == message.WorkerInstanceId);

		if (workContainer == null) throw new ArgumentOutOfRangeException($"Worker container with workerInstanceId = {message.WorkerInstanceId} not found");
		if (workContainer.Status != WorkerInstanceExecutionStatus.NotStarted &&
			workContainer.Status != WorkerInstanceExecutionStatus.Failed &&
			workContainer.Status != WorkerInstanceExecutionStatus.Aborted &&
			workContainer.Status != WorkerInstanceExecutionStatus.FinishedSuccessfully)
		{
			throw new InvalidOperationException($"Cannot restart worker instance with ID={workContainer.WorkerInstanceId} it's running, you should stop it before, or wait until finish");
		}

		await workersDispatcher.ReRunWorkerInstance(message.jsonInput, workContainer, cancelToken);

		await bus.PublishFromHostToServer(new RestartWorkerResponse
		{
			Id = message.Id,
			HostId = message.HostId,
			WorkerInstanceId = workContainer.WorkerInstanceId,
			Processed = true
		}, cancelToken);
	}
}

