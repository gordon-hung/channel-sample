namespace ChannelSample.AppHost.Models;

public record ChannelInfo(
	int Id,
	string Message,
	int Sequence,
	DateTimeOffset MessageAt,
	DateTimeOffset CreatedAt);
