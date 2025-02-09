using ChannelSample.AppHost.Models;

namespace ChannelSample.AppHost;

public interface IChannelRepo
{
	public Task InitialAsync(CancellationToken cancellationToken = default);

	public Task InsertAsync(string application, string message, int sequence, DateTimeOffset messageAt, CancellationToken cancellationToken = default);

	public IAsyncEnumerable<ChannelInfo> QueryAsync(CancellationToken cancellationToken = default);
}
