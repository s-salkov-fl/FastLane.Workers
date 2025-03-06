using FastLane.Workers.Bucket.Services;
using FastLane.Workers.Configuration;
using FastLane.Workers.Contract.Services;
using FastLane.Workers.Contract.Utils;
using FastLane.Workers.Models;
using Microsoft.Extensions.Options;

namespace FastLane.Workers.Bucket.Consumers;

public class FileDownloadResponseConsumer : WorkerBusConsumer<FileDownloadResponse>
{
	private readonly HostConfiguration hostConfig;
	private readonly WorkersDispatcher workersDispatcher;

	public FileDownloadResponseConsumer(ILogger<FileDownloadResponse> logger,
		IOptions<HostConfiguration> hostConfig,
		IOptions<BusConfiguration> busConfiguration,
		WorkersDispatcher workersDispatcher) : base(busConfiguration, logger)
	{
		this.hostConfig = hostConfig.Value;
		this.workersDispatcher = workersDispatcher;
	}

	public override async Task Consume(IWorkerBus bus, FileDownloadResponse message, CancellationToken cancelToken)
	{
		string assemblyZipFileName = AssemblyFileUtils.GetZipName(hostConfig.WorkersAssemblyFolder, message.WorkerAssemblyName);
		AssemblyFileUtils.SaveAssemblyZip(message.FileBody, hostConfig.WorkersAssemblyFolder, message.WorkerAssemblyName);

		if (!AssemblyFileUtils.AssemblyPackedExists(hostConfig.WorkersAssemblyFolder, message.WorkerAssemblyName))
		{
			throw new DirectoryNotFoundException($"Assembly received from server was not found in: '{assemblyZipFileName}' after SAVING");
		}

		AssemblyFileUtils.DecompressAssemblyFolder(hostConfig.WorkersAssemblyFolder, message.WorkerAssemblyName);

		if (!AssemblyFileUtils.AssemblyDistributiveExists(hostConfig.WorkersAssemblyFolder, message.WorkerAssemblyName))
		{
			throw new DirectoryNotFoundException($"Assembly's folder was not found: '{AssemblyFileUtils.GetFolderName(hostConfig.WorkersAssemblyFolder, message.WorkerAssemblyName)}' after EXTRACTION");
		}

		WorkerContainer workContainer = workersDispatcher.workersInfo.FirstOrDefault(w => w.WorkerInstanceId == message.WorkerInstanceId);

		if (workContainer == null) throw new ArgumentOutOfRangeException($"Worker container with workerInstanceId = {message.WorkerInstanceId} not found");

		if (workContainer.Status == WorkerInstanceExecutionStatus.Running)
		{
			throw new InvalidOperationException($"Cannot restart worker instance with ID={workContainer.WorkerInstanceId} it's running, you should stop it before, or wait until finish");
		}

		await workersDispatcher.ReRunWorkerInstance(null as string, workContainer, cancelToken);
	}
}


