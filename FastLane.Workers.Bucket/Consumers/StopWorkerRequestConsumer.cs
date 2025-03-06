using FastLane.Workers.Bucket.Services;
using FastLane.Workers.Configuration;
using FastLane.Workers.Contract.Services;
using FastLane.Workers.Contracts;
using FastLane.Workers.Models;
using Microsoft.Extensions.Options;

namespace FastLane.Workers.Bucket.Consumers;

public class StopWorkerRequestConsumer : WorkerBusConsumer<StopWorkerRequest>
{
	private readonly ILogger<StopWorkerRequestConsumer> _logger;
	private readonly WorkersDispatcher workersDispatcher;

	public StopWorkerRequestConsumer(ILogger<StopWorkerRequestConsumer> logger,
		IOptions<BusConfiguration> busConfiguration,
		WorkersDispatcher workersDispatcher) : base(busConfiguration, logger)
	{
		_logger = logger;
		this.workersDispatcher = workersDispatcher;
	}

	public override async Task Consume(IWorkerBus bus, StopWorkerRequest message, CancellationToken cancelToken)
	{
		workersDispatcher.StopWorkerInstance(message.WorkerInstanceId);

		await bus.PublishFromHostToServer(new StopWorkerResponse
		{
			Id = message.Id,
			HostId = message.HostId,
			WorkerInstanceId = message.WorkerInstanceId,
			Processed = true
		}, cancelToken);
	}
}
