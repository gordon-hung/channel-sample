namespace ChannelSample.AppHost;

public record ChannelSettingsOptions
{
    public int Expiry { get; init; } = 900;
}
