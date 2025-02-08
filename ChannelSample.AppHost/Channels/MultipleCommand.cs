namespace ChannelSample.AppHost.Channels;

public record MultipleCommand(
	string Application,
	DateTimeOffset MessageAt,
	string Message);