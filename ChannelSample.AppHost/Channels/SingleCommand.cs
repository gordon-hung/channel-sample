namespace ChannelSample.AppHost.Channels;

public record SingleCommand(
	DateTimeOffset MessageAt,
	string Message);