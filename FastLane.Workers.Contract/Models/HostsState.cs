using System.Collections.Concurrent;
using System.Diagnostics;

namespace FastLane.Workers.Models;

public class HostsState
{
	public DateTimeOffset HostStatesCacheObtainTime { get; set; }

	private ConcurrentBag<HostStatus> hostStatesRecent = new ConcurrentBag<HostStatus>();
	private ConcurrentBag<HostStatus> hostStatesActual = new ConcurrentBag<HostStatus>();
	
	private readonly Stopwatch stopWatch = new Stopwatch();
	private readonly int waitTimeOutMilliseconds;

	private volatile bool isModifying = false;

	public IEnumerable<HostStatus> HostsStateCache
	{
		get
		{
			return GetHostStates();
		}
	}

	public HostsState(int waitTimeOutMilliseconds) 
	{
		this.waitTimeOutMilliseconds = waitTimeOutMilliseconds;
	}

	public void StartModifying()
	{
		isModifying = true;

		foreach (HostStatus hostStatusActual in hostStatesActual)
			hostStatusActual.HostHealth = HostServiceHealthStatus.Offline;

		var unionStat = hostStatesActual.Union(hostStatesRecent);
		hostStatesRecent = new ConcurrentBag<HostStatus>(unionStat);
	}

	public void EndModifying()
	{
		var unionStat = hostStatesActual.Union(hostStatesRecent);
		//Console.WriteLine();
		//Console.WriteLine("EndMod actual:");
		//foreach (var hostStat in hostStatesActual) Console.WriteLine($"hostId={hostStat.HostId} hostProcess={hostStat.HostProcessId} host={hostStat.HostHealth}");
		//Console.WriteLine("EndMod recent:");
		//foreach (var hostStat in hostStatesRecent) Console.WriteLine($"hostId={hostStat.HostId} hostProcess={hostStat.HostProcessId} host={hostStat.HostHealth}");
		//Console.WriteLine("EndMod union:");
		//foreach (var hostStat in unionStat) Console.WriteLine($"hostId={hostStat.HostId} hostProcess={hostStat.HostProcessId} host={hostStat.HostHealth}");
		isModifying = false;
	}

	public bool WaitEndModifying(bool throwIfTimeOutElapses = false, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (isModifying)
		{
			stopWatch.Restart();
			while (isModifying && 
				!cancellationToken.IsCancellationRequested &&
				stopWatch.ElapsedMilliseconds < waitTimeOutMilliseconds)
			while(true)
			{
				Task.Delay(100).Wait();
			}

			stopWatch.Stop();
		}

		if (!isModifying) return true;

		if (throwIfTimeOutElapses)
		{
			throw new TimeoutException("Time out for state obtain expired");
		}
		else
		{
			return false;
		}
	}

	public IEnumerable<HostStatus> GetHostStates(bool onlyActual = false)
	{
		if (onlyActual)
		{
			WaitEndModifying(true);
			return hostStatesActual;
		}
		else
		{
			return hostStatesActual.Union(hostStatesRecent);
		}
	}

	public IEnumerable<HostStatus> FindHostStates(Func<HostStatus, bool> predicate, bool onlyActual = false)
	{
		if (predicate == null) throw new ArgumentNullException(nameof(predicate));

		if (onlyActual)
		{
			WaitEndModifying(true);
			return hostStatesActual.Where(predicate);
		}
		else
		{
			return hostStatesActual.Union(hostStatesRecent).Where(predicate);
		}
	}

	public void Clear(bool clearBothActualAndRecent=false)
	{
		if (!isModifying) throw new InvalidOperationException("Before update stats need to call StartModifying method");
		hostStatesActual.Clear();
		if (clearBothActualAndRecent) hostStatesRecent.Clear();
	}

	/// <summary>
	/// Add new status of the Host into stats cache
	/// </summary>
	/// <exception cref="InvalidOperationException">If did not call StartModifying before</exception>
	public void AddHostStatus(HostStatus newStatus)
	{
		if (newStatus == null) throw new ArgumentNullException(nameof(newStatus));
		if (!isModifying) throw new InvalidOperationException("Before update stats need to call StartModifying method");

		hostStatesActual.Add(newStatus);
		HostStatesCacheObtainTime = DateTimeOffset.Now;
	}

	/// <summary>
	/// Add new status of the Host into stats cache
	/// </summary>
	/// <exception cref="InvalidOperationException">If did not call StartModifying before</exception>
	public void DeleteHostStatuses(Func<HostStatus, bool> predicate)
	{
		if (predicate == null) throw new ArgumentNullException(nameof(predicate));
		if (!isModifying) throw new InvalidOperationException("Before update stats need to call StartModifying method");
		if (hostStatesActual.Count == 0) return;

		DeleteHostStatuses(hostStatesActual.Where(predicate));
	}

	public void DeleteHostStatuses(IEnumerable<HostStatus> hostStates)
	{
		if (hostStates == null) throw new ArgumentNullException(nameof(hostStates));
		if (!isModifying) throw new InvalidOperationException("Before update stats need to call StartModifying method");
		if (this.hostStatesActual.Count == 0) return;

		foreach (var hostState in hostStates)
		{
			this.hostStatesActual.TryTake(out HostStatus hostStatus);
		}
	}
}
