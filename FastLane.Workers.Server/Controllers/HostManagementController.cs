using FastLane.Workers.Models;
using FastLane.Workers.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace FastLane.Workers.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class HostManagementController : ControllerBase
{
	private readonly ILogger<HostManagementController> _logger;
	private readonly HostsStatsCacheManager hostsStatsCacheManager;
	private readonly HostsState hostsState;

	public HostManagementController(ILogger<HostManagementController> logger,
		HostsStatsCacheManager hostsStatsCacheManager,
		HostsState hostsState)
	{
		_logger = logger;
		this.hostsStatsCacheManager = hostsStatsCacheManager;
		this.hostsState = hostsState;
	}

	/// <summary>
	/// Refresh server's sost statuses cache and get stats info about Host, including list of host's worker instances
	/// </summary>
	/// <param name="hostId">Id of Host</param>
	/// <returns>Host status information founded in host statuses cache</returns>
	[HttpGet("{hostId}")]
	public async Task<IActionResult> GetHostStatus(string hostId, CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(hostId = hostId.Trim())) return BadRequest(nameof(hostId));

		await hostsStatsCacheManager.RefreshHostsStatsCache(cancellationToken);

		return Ok(hostsState.FindHostStates(h=>h.HostId == hostId));
	}

}