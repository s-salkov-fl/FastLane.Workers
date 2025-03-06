using FastLane.Workers.Contract.Exceptions;
using FastLane.Workers.Contracts;
using System.Collections.Concurrent;
using System.Text.Json;

namespace FastLane.Workers.Bucket.Services
{
	public class WorkersDispatcher
	{
		public ConcurrentBag<WorkerContainer> workersInfo { get; } = new ConcurrentBag<WorkerContainer>();
		private readonly WorkerFactory workerFactory;
		private readonly IServiceProvider serviceProvider;
		private readonly ILogger<WorkersDispatcher> logger;

		public WorkersDispatcher(WorkerFactory workerFactory,
			 IServiceProvider provider,
			 ILogger<WorkersDispatcher> logger)
		{
			this.workerFactory = workerFactory;
			this.logger = logger;
			serviceProvider = provider;
		}

		public void StopWorkerInstance(Guid workerInstanceId)
		{
			WorkerContainer? workerContainer = workersInfo.FirstOrDefault(w => w.WorkerInstanceId == workerInstanceId);

			if (workerContainer != null)
			{
				workerContainer.cancellationTokenSource.Cancel();
			}
			else
			{
				throw new ArgumentOutOfRangeException($"Worker container with workerInstanceId = {workerInstanceId} not found");
			}
		}

		public async Task<WorkerContainer> RunWorkerInstance(string WorkerTypeName, JsonElement? input, CancellationToken cancellationToken)
		{
			IWorker worker;
			WorkerContainer workerContainer;

			try
			{
				worker = workerFactory.CreateWorkerInstance(WorkerTypeName);
				workerContainer = new WorkerContainer(worker, serviceProvider.GetService<ILogger<WorkerContainer>>());
				workersInfo.Add(workerContainer);
			}
			catch (WorkerAssemblyNotFoundException ex)
			{
				workerContainer = new WorkerContainer(null, serviceProvider.GetService<ILogger<WorkerContainer>>());
				workerContainer.Status = Models.WorkerInstanceExecutionStatus.NoAssembly;
				workerContainer.WorkerTypeName = WorkerTypeName;
				workersInfo.Add(workerContainer);
				ex.WorkerContainer = workerContainer;

				throw;
			}

			Task.Run(() => workerContainer.RunAsync(input, workerContainer.cancellationTokenSource.Token));
			return await Task.FromResult<WorkerContainer>(workerContainer);
		}

		public async Task<WorkerContainer> RunWorkerInstance(string WorkerTypeName, string input, CancellationToken cancellationToken)
		{
			JsonElement? inpJson = null;

			if (!string.IsNullOrEmpty(input))
			{
				using (JsonDocument doc = JsonDocument.Parse(input))
				{
					inpJson = doc.RootElement.Clone();
				}
			}

			return await RunWorkerInstance(WorkerTypeName, inpJson, cancellationToken);
		}

		public async Task<WorkerContainer> ReRunWorkerInstance(JsonElement? input, WorkerContainer workingContainer, CancellationToken cancellationToken)
		{
			workingContainer.cancellationTokenSource = new CancellationTokenSource();

			if (workingContainer.Worker == null)
			{
				try
				{
					workingContainer.Worker = workerFactory.CreateWorkerInstance(workingContainer.WorkerTypeName);
				}
				catch (WorkerAssemblyNotFoundException ex)
				{
					logger.LogError($"Restart worker type '{ex.WorkerType}' with id='{((WorkerContainer)ex.WorkerContainer).WorkerInstanceId}' failed due to absence of assembly:'{ex.AssemblyName}'");
					workingContainer.Status = Models.WorkerInstanceExecutionStatus.Failed;
				}
			}

			if (workingContainer.Worker == null) workingContainer.Status = Models.WorkerInstanceExecutionStatus.Failed;

			Task.Run(() => workingContainer.RunAsync(input ?? workingContainer.Input, workingContainer.cancellationTokenSource.Token));
			return workingContainer;
		}

		public async Task<WorkerContainer> ReRunWorkerInstance(string input, WorkerContainer workingContainer, CancellationToken cancellationToken)
		{
			JsonElement? inpJson = null;

			if (!string.IsNullOrEmpty(input) && !input.ToLower().StartsWith("none"))
			{
				using (JsonDocument doc = JsonDocument.Parse(input))
				{
					inpJson = doc.RootElement.Clone();
				}
			}

			return await ReRunWorkerInstance(inpJson, workingContainer, cancellationToken);
		}
	}
}
