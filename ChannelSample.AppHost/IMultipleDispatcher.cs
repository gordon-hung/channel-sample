using ChannelSample.AppHost.Models;

namespace ChannelSample.AppHost;

public interface IMultipleDispatcher
{
	ValueTask PushCommandAsync(ChannelCommand command, CancellationToken cancellationToken = default);

	void ExtendExpiry(string application);
}
