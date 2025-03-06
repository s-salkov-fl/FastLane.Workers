using FastLane.Workers.Configuration;
using FastLane.Workers.Contract.Services;
using FastLane.Workers.Contracts;
using FastLane.Workers.Models;
using Microsoft.Extensions.Options;

namespace FastLane.Workers.Server.Consumers
{
	public class WorkerInstanceStatusResponseConsumer : WorkerBusConsumer<HostWorkerInstanceStatusResponse>
	{
		private readonly ILogger<WorkerInstanceStatusResponseConsumer> _logger;
		private readonly WorkerInstancesState workerInstancesState;

		public WorkerInstanceStatusResponseConsumer(ILogger<WorkerInstanceStatusResponseConsumer> logger,
			WorkerInstancesState workerInstancesState,
			IOptions<BusConfiguration> busConfiguration) : base(busConfiguration, logger)
		{
			_logger = logger;
			this.workerInstancesState = workerInstancesState;
		}

		public override Task Consume(IWorkerBus bus, HostWorkerInstanceStatusResponse message, CancellationToken cancelToken)
		{
			workerInstancesState.WorkerInstanceStatuses.RemoveAll(w => w.HostId == message.HostId && w.InstanceId == message.InstanceId);

			workerInstancesState.WorkerInstanceStatuses.Add(
					new WorkerInstanceStatus
					{
						LastResponseId = message.Id,
						HostId = message.HostId,
						InstanceId = message.InstanceId,
						TypeId = message.TypeId,
						Input = message.Input,
						Result = message.Result,
						Status = message.Status
					}); ;

			return Task.CompletedTask;
		}
	}
}
