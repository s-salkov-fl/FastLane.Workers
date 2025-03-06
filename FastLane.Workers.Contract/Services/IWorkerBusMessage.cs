using System.Text.Json.Serialization;

namespace FastLane.Workers.Contract.Services;

public interface IWorkerBusMessage
{
	[JsonIgnore]
	string ContentEncoding { get; set; }

	[JsonIgnore]
	string ContentType { get; set; }

	[JsonIgnore]
	long TimeStamp { get; set; }

	[JsonIgnore]
	string Expiration { get; set; }

	[JsonIgnore]
	bool IsExpired { get
		{
			var timestamp = TimeStamp;
			var dateSended = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
			long expiredMs;

			if (!string.IsNullOrEmpty(Expiration) && (expiredMs = long.Parse(Expiration)) != -1)
			{
				var dateExpire = dateSended.AddMilliseconds(expiredMs);
				if (dateExpire < DateTimeOffset.Now) return true;
			}

			return false;
		}
	}
}
