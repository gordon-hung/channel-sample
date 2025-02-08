using ChannelSample.AppHost.Channels;

namespace ChannelSample.AppHost;

public interface IMultipleDispatcher
{
	ValueTask PushCommandAsync(MultipleCommand command, CancellationToken cancellationToken = default);

	void ExtendExpiry(string application);
}