using Microsoft.Extensions.Options;
using FastLane.Workers.Configuration;
using FastLane.Workers.Contracts;

namespace FastLane.Workers.Bucket.Services
{
	public class WorkerFactory
	{
		private readonly WorkersAssemblyLoadContext workerLoadContext;
		private readonly HostConfiguration hostConfiguration;
		private readonly ILogger<WorkerFactory> loggerFactory;
		private readonly ILogger<WorkersAssemblyLoadContext> loggerAssemblyLoad;
		private readonly IServiceProvider serviceProvider;

		public WorkerFactory(IOptions<HostConfiguration> hostConfiguration, 
			ILogger<WorkerFactory> loggerFactory,
			ILogger<WorkersAssemblyLoadContext> loggerAssemblyLoad,
			IServiceProvider serviceProvider)
		{
			this.hostConfiguration = hostConfiguration.Value;
			workerLoadContext = new WorkersAssemblyLoadContext(loggerAssemblyLoad, this.hostConfiguration.WorkersAssemblyFolder);
			this.serviceProvider = serviceProvider;
		}

		public IWorker CreateWorkerInstance(string workerTypeName)
		{
			if (string.IsNullOrEmpty(workerTypeName)) throw new ArgumentNullException(nameof(workerTypeName));

			var workerTypeNameOnly = workerLoadContext.GetWorkerTypeNameOnly(workerTypeName);
			var workersAssembly = workerLoadContext.LoadWorker(workerTypeName) 
				?? throw new InvalidOperationException("Unable to load assembly for worker " + workerTypeName);

			var workerType = workersAssembly.GetType(workerTypeNameOnly)
				?? throw new MethodAccessException($"Worker type with Id {workerTypeName} was NOT found in Host"); ;

			return ActivatorUtilities.CreateInstance(serviceProvider, workerType) as IWorker
				?? throw new MethodAccessException($"Worker type with Id {workerTypeName} was found in Host. But failed to construct instance of type {workerType.FullName}");
		}
	}
}
