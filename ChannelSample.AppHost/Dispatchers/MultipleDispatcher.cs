using System.Collections.Concurrent;
using ChannelSample.AppHost.Channels;
using ChannelSample.AppHost.Models;

namespace ChannelSample.AppHost.Dispatchers;

internal class MultipleDispatcher(IServiceProvider serviceProvider) : IMultipleDispatcher, IDisposable, IAsyncDisposable
{
	private readonly ConcurrentDictionary<string, MultipleChannel> _counters = new();
	private readonly CancellationTokenSource _cancellationTokenSource = new();

	private bool _disposedValue;

	~MultipleDispatcher()
	{
		Dispose(false);
	}

	public async ValueTask PushCommandAsync(ChannelCommand command, CancellationToken cancellationToken = default)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

		var counter = _counters.GetOrAdd(
			string.Concat(command.Application),
			key =>
			{
				var scope = serviceProvider.CreateScope();
				var newCounter = ActivatorUtilities.CreateInstance<MultipleChannel>(
					scope.ServiceProvider,
					key);

				newCounter.Disposed += (_, _) =>
				{
					_ = _counters.TryRemove(newCounter.Key, out _);
					scope.Dispose();
				};

				return newCounter;
			});

		await counter.PrepareAsync(cts.Token).ConfigureAwait(false);
		await counter.PushCommandAsync(command, cts.Token).ConfigureAwait(false);
	}

	public ValueTask DisposeAsync()
	{
		Dispose(true);
		GC.SuppressFinalize(this);

		return ValueTask.CompletedTask;
	}

	public void Dispose()
	{
		// 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public void ExtendExpiry(string application)
	{
		if (_counters.TryGetValue(string.Concat(application), out var counter))
			counter.ExtendExpiry();
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
				_cancellationTokenSource.Cancel();

			_cancellationTokenSource.Dispose();
			_disposedValue = true;
		}
	}
}
