using FastLane.Workers.Server.Services;
using FastLane.Workers.Models;
using Microsoft.AspNetCore.Mvc;

namespace FastLane.Workers.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class ServerManagementController : ControllerBase
{
	private readonly ILogger<ServerManagementController> _logger;
	private readonly ServerStatus serverStatus;
	private HostsState hostsState;
	private HostsStatsCacheManager hostsCacheManager;

	public ServerManagementController(ILogger<ServerManagementController> logger,
		ServerStatus serverStatus,
		HostsState hostsState,
		HostsStatsCacheManager hostsCacheManager)
	{
		_logger = logger;
		this.serverStatus = serverStatus;
		this.hostsState = hostsState;
		this.hostsCacheManager = hostsCacheManager;
	}

	/// <summary>
	/// Returns state information about hosts being online recently
	/// </summary>
	[HttpGet]
	public async Task<IActionResult> GetServerStatus(CancellationToken cancellationToken)
	{
		return Ok(serverStatus);
	}

	/// <summary>
	/// Total clear host status cache and fill with new values
	/// </summary>
	[HttpGet("FixIssues")]
	public async Task<IActionResult> FixServerHostStats(CancellationToken cancellationToken)
	{
		hostsState.StartModifying();
		hostsState.Clear(true);
		hostsState.EndModifying();

		await hostsCacheManager.RefreshHostsStatsCache(cancellationToken);
		await hostsCacheManager.RefreshHostsStatsCache(cancellationToken);//send ping two times to find if second host still online and answers second ping

		return Ok(serverStatus);
	}

}