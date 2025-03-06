using FastLane.Workers.Configuration;
using FastLane.Workers.Contract.Services;
using FastLane.Workers.Contract.Utils;
using FastLane.Workers.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net;

namespace FastLane.Workers.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class WorkersManagementController : ControllerBase
{
	private readonly ILogger<HostManagementController> _logger;
	private readonly IWorkerBus publishEndpoint;
	private readonly long waitBusResponseTimeMilliseconds;
	private readonly WorkerInstancesState workerInstancesState;
	private readonly RunNewWorkersState runNewWorkersState;
	private readonly StopWorkersState stopWorkersState;
	private readonly RestartWorkersState restartWorkersState;
	private readonly HostsState hostsState;
	private readonly ServerStatus serverStatus;
	private readonly ServerConfiguration serverConfiguration;

	public WorkersManagementController(ILogger<HostManagementController> logger,
		IOptions<BusConfiguration> busConfiguration,
		IOptions<ServerConfiguration> serverConfiguration,
		IWorkerBus publishEndpoint,
		HostsState hostsState,
		WorkerInstancesState workerInstancesState,
		RunNewWorkersState runNewWorkersState,
		StopWorkersState stopWorkersState,
		RestartWorkersState restartWorkersState,
		ServerStatus serverStatus)
	{
		_logger = logger;
		this.publishEndpoint = publishEndpoint;
		this.workerInstancesState = workerInstancesState;
		this.runNewWorkersState = runNewWorkersState;
		this.stopWorkersState = stopWorkersState;
		this.restartWorkersState = restartWorkersState;
		this.hostsState = hostsState;
		this.serverStatus = serverStatus;
		this.serverConfiguration = serverConfiguration.Value;
		waitBusResponseTimeMilliseconds = busConfiguration.Value.ResponseWaitTimeMilliseconds;
	}

	/// <summary>
	/// Get detailed information about worker instance of the host
	/// </summary>
	/// <param name="hostId">Id of the host</param>
	/// <param name="workerInstanceId">Guid of worker instance. Can be obtained with ServerManagement or HostManagement methods </param>
	[HttpGet("HostWorker/{hostId}/{workerInstanceId}")]
	public async Task<IActionResult> GetHostWorker(string hostId, string workerInstanceId, CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(hostId = hostId.Trim())) return BadRequest(nameof(hostId));
		if (string.IsNullOrEmpty(workerInstanceId = workerInstanceId.Trim())) return BadRequest(nameof(workerInstanceId));

		var hostConflicts = serverStatus.FindCriticalProblemHost(hostId);
		if (hostConflicts.Any())
		{
			return new JsonResult(hostConflicts) { StatusCode = (int)HttpStatusCode.InternalServerError };
		}

		if (!Guid.TryParse(workerInstanceId, out Guid guidWorkerInstanceId)) return BadRequest($"Invalid Guid of {nameof(workerInstanceId)}");

		var requestId = Guid.NewGuid();

		await publishEndpoint.PublishFromServerToHostDirect(
			new HostWorkerInstanceStatusRequest
			{
				Id = requestId,
				HostId = hostId,
				WorkerInstanceId = guidWorkerInstanceId,
			}, hostId, cancellationToken);

		var waitResponseTask = Task<IActionResult>.Run(() =>
		{
			Stopwatch chrono = new Stopwatch();
			chrono.Start();

			while (!cancellationToken.IsCancellationRequested && chrono.ElapsedMilliseconds < waitBusResponseTimeMilliseconds)
			{
				var workerInstanceStatus = workerInstancesState.WorkerInstanceStatuses.Find(w => w.LastResponseId == requestId);

				if (workerInstanceStatus != null)
				{
					return Ok(workerInstanceStatus);
				}
				Task.Delay(TimeSpan.FromSeconds(0.5), cancellationToken).Wait(cancellationToken);
			}

			return (!cancellationToken.IsCancellationRequested) ? Problem(detail: "Request to worker instance has timed out", statusCode: 503, title: "Bus timeout")
			: Problem(detail: "Request was canceled", statusCode: 200, title: "Request cancel");
		});

		await waitResponseTask;
		return waitResponseTask.Result;
	}

	/// <summary>
	/// Run new instance of worker at the target host
	/// </summary>
	/// <param name="hostId">Id of the host</param>
	/// <param name="workerTypeName">Worker's type name in assembly qualified format</param>
	/// <param name="jsonInput">Input configuration parameters for worker in Json format</param>
	[HttpPost("Run")]
	public async Task<IActionResult> Run(string hostId, string workerTypeName, string jsonInput, CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(hostId = hostId.Trim())) return BadRequest(nameof(hostId));
		if (string.IsNullOrEmpty(workerTypeName = workerTypeName.Trim())) return BadRequest(nameof(workerTypeName));
		
		var hostConflicts = serverStatus.FindCriticalProblemHost(hostId);
		if (hostConflicts.Any())
		{
			return new JsonResult(hostConflicts) { StatusCode = (int)HttpStatusCode.InternalServerError };
		}

		var requestId = Guid.NewGuid();

		await publishEndpoint.PublishFromServerToHostDirect(
			new RunNewWorkerRequest
			{
				Id = requestId,
				HostId = hostId,
				WorkerTypeName = workerTypeName,
				jsonInput = jsonInput,
			}, hostId, cancellationToken);

		Stopwatch chrono = new Stopwatch();
		chrono.Start();

		var waitResponseTask = Task.Run(() =>
		{
			while (!cancellationToken.IsCancellationRequested && chrono.ElapsedMilliseconds < waitBusResponseTimeMilliseconds)
			{
				RunNewWorkerStatus runWorkerStatus = runNewWorkersState.RunWorkerStates.GetValueOrDefault(requestId);
				if (runWorkerStatus != null)
				{
					runNewWorkersState.RunWorkerStates.Remove(requestId, out RunNewWorkerStatus _tmp);

					if (runWorkerStatus.NeedAssembly)
					{
						var rootFolder = serverConfiguration.WorkersAssemblyFolder;
						var relativeFolder = runWorkerStatus.AssemblyLocation;
						if (!AssemblyFileUtils.AssemblyPackedExists(rootFolder, relativeFolder))
						{
							if (AssemblyFileUtils.AssemblyDistributiveExists(rootFolder, relativeFolder))
							{
								AssemblyFileUtils.CompressAssemblyFolder(serverConfiguration.WorkersAssemblyFolder, runWorkerStatus.AssemblyLocation);
							}
							else
							{
								Problem(detail: $"Worker's requests to get Assembly Failed: Unable to read file after compression '{AssemblyFileUtils.GetZipName(rootFolder, relativeFolder)}'"
								, statusCode: 503, title: "File Error");
							}
						}

						if (AssemblyFileUtils.AssemblyPackedExists(rootFolder, relativeFolder))
						{
							try
							{
								byte[] fileContents = System.IO.File.ReadAllBytes(AssemblyFileUtils.GetZipName(rootFolder, relativeFolder));

								publishEndpoint.PublishFromServerToHostDirect(
									new FileDownloadResponse
									{
										Id = requestId,
										HostId = hostId,
										WorkerInstanceId = runWorkerStatus.WorkerInstanceId,
										WorkerAssemblyName = runWorkerStatus.AssemblyLocation,
										FileBody = fileContents
									}, hostId, cancellationToken);
							}
							catch (Exception ex)
							{
								Problem(detail: $"Worker's requests to get Assembly Failed: Unable to read file'{AssemblyFileUtils.GetZipName(rootFolder, relativeFolder)}'"
									+ Environment.NewLine + ex.Message
								, statusCode: 503, title: "File Error");
							}
						}
						else
						{
							Problem(detail: $"Worker's requests to get Assembly Failed: Unable to read file after compression '{AssemblyFileUtils.GetZipName(rootFolder, relativeFolder)}'"
								, statusCode: 503, title: "File Error");
						}
					}

					return Ok(runWorkerStatus);
				}
				Task.Delay(TimeSpan.FromSeconds(0.5), cancellationToken).Wait();
			}

			return (!cancellationToken.IsCancellationRequested) ? Problem(detail: "Request to running worker instance has timed out", statusCode: 503, title: "Bus timeout")
			: Problem(detail: "Request was canceled", statusCode: 200, title: "Request cancel");
		});

		await waitResponseTask;
		return waitResponseTask.Result;
	}

	/// <summary>
	/// Stops instance of worker at the target host
	/// </summary>
	/// <param name="hostId">Id of the host</param>
	/// <param name="workerInstanceId">Guid of worker instance. Can be obtained with ServerManagement or HostManagement methods </param>
	[HttpPut("Stop")]
	public async Task<IActionResult> Stop(string hostId, string workerInstanceId, CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(hostId = hostId.Trim())) return BadRequest(nameof(hostId));
		if (string.IsNullOrEmpty(workerInstanceId = workerInstanceId.Trim())) return BadRequest(nameof(workerInstanceId));

		var hostConflicts = serverStatus.FindCriticalProblemHost(hostId);
		if (hostConflicts.Any())
		{
			return new JsonResult(hostConflicts) { StatusCode = (int)HttpStatusCode.InternalServerError };
		}

		if (!Guid.TryParse(workerInstanceId, out Guid workerInstanceIdGuid)) return BadRequest("workerInstanceId - Bad Guid");

		var requestId = Guid.NewGuid();

		await publishEndpoint.PublishFromServerToHostDirect(
			new StopWorkerRequest
			{
				Id = requestId,
				HostId = hostId,
				WorkerInstanceId = workerInstanceIdGuid
			}, hostId, cancellationToken);

		Stopwatch chrono = new Stopwatch();
		chrono.Start();

		var waitResponseTask = Task.Run(() =>
		{
			while (!cancellationToken.IsCancellationRequested && chrono.ElapsedMilliseconds < waitBusResponseTimeMilliseconds)
			{
				StopWorkerStatus stopWorkerStatus = stopWorkersState.StopWorkerStates.Find(s=> s.LastResponseId == requestId);
				if (stopWorkerStatus != null)
				{
					return Ok(stopWorkerStatus);
				}
				Task.Delay(TimeSpan.FromSeconds(0.5), cancellationToken).Wait();
			}

			return (!cancellationToken.IsCancellationRequested) ? Problem(detail: "Request to stopped worker instance has timed out", statusCode: 503, title: "Bus timeout")
			: Problem(detail: "Request was canceled", statusCode: 200, title: "Request cancel");
		});

		await waitResponseTask;
		return waitResponseTask.Result;
	}

	/// <summary>
	/// Restarts instance of worker that was finished before or failed
	/// </summary>
	/// <param name="hostId">Id of the host</param>
	/// <param name="workerInstanceId">Guid of worker instance. Can be obtained with ServerManagement or HostManagement methods </param>
	/// <param name="jsonInput">New Json input worker configuration parameters, if needed to change, or 'NONE' value to leave old</param>
	[HttpPut("Restart")]
	public async Task<IActionResult> Restart(string hostId, string workerInstanceId, string jsonInput, CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(hostId = hostId.Trim())) return BadRequest(nameof(hostId));
		if (string.IsNullOrEmpty(workerInstanceId = workerInstanceId.Trim())) return BadRequest(nameof(workerInstanceId));

		var hostConflicts = serverStatus.FindCriticalProblemHost(hostId);
		if (hostConflicts.Any())
		{
			return new JsonResult(hostConflicts) { StatusCode = (int)HttpStatusCode.InternalServerError };
		}

		if (!Guid.TryParse(workerInstanceId, out Guid workerInstanceIdGuid))
		{
			return BadRequest("workerInstanceId - Bad Guid");
		}
		var requestId = Guid.NewGuid();

		await publishEndpoint.PublishFromServerToHostDirect(
			new RestartWorkerRequest
			{
				Id = requestId,
				HostId = hostId,
				WorkerInstanceId = workerInstanceIdGuid,
				jsonInput = jsonInput
			}, hostId, cancellationToken);

		Stopwatch chrono = new Stopwatch();
		chrono.Start();

		var waitResponseTask = Task.Run(() =>
		{
			while (!cancellationToken.IsCancellationRequested && chrono.ElapsedMilliseconds < waitBusResponseTimeMilliseconds)
			{
				RestartWorkerStatus RestartWorkerStatus = restartWorkersState.RestartWorkerStates.Find(s => s.LastResponseId == requestId);
				if (RestartWorkerStatus != null)
				{
					return Ok(RestartWorkerStatus);
				}
				Task.Delay(TimeSpan.FromSeconds(0.5), cancellationToken).Wait();
			}

			return (!cancellationToken.IsCancellationRequested) ? Problem(detail: "Request to Restartped worker instance has timed out", statusCode: 503, title: "Bus timeout")
			: Problem(detail: "Request was canceled", statusCode: 200, title: "Request cancel");
		});

		await waitResponseTask;
		return waitResponseTask.Result;
	}
}