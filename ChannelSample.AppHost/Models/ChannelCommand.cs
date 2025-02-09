namespace ChannelSample.AppHost.Models;

public record ChannelCommand(
    string Application,
    DateTimeOffset MessageAt,
    string Message,
    int Sequence);
