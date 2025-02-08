using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace ChannelSample.AppHost.Channels;

internal class MultipleChannel(
	ILogger<MultipleChannel> logger,
	IOptions<ChannelSettingsOptions> options,
	string key) : IDisposable, IAsyncDisposable
{
	private static readonly ActivitySource _ActivitySource = new(name: typeof(MultipleChannel).FullName!);

	private readonly Channel<MultipleCommand> _channel = Channel.CreateUnbounded<MultipleCommand>(
		new UnboundedChannelOptions
		{
			SingleReader = true
		});

	private readonly SemaphoreSlim _locker = new(1, 1);
	private readonly CancellationTokenSource _cancellationTokenSource = new();

	private Task? _backgroundTask;
	private int _expiry = options.Value.Expiry;
	private Timer? _timer;
	private bool _disposedValue;

	public event EventHandler? Disposed;

	public string Key { get; } = key;

	~MultipleChannel()
	{
		// 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
		Dispose(disposing: false);
	}

	public async ValueTask PrepareAsync(CancellationToken cancellationToken = default)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

		if (_backgroundTask is not null)
			return;

		await _locker.WaitAsync(cts.Token).ConfigureAwait(false);

		try
		{
			if (_backgroundTask is not null)
				return;

			_timer = new Timer(_ =>
			{
				_ = Interlocked.Decrement(ref _expiry);

				logger.LogInformation("Key:{key} 倒數時間:{expiry}", Key, _expiry);

				if (_expiry <= 0)
					Dispose();
			}, null, 0, 1000);

			_backgroundTask = Task.Run(
				() => ProcessCommandLoopAsync(_cancellationTokenSource.Token),
				_cancellationTokenSource.Token);
		}
		finally
		{
			_ = _locker.Release();
		}
	}

	public async ValueTask PushCommandAsync(MultipleCommand command, CancellationToken cancellationToken = default)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(
			cancellationToken,
			_cancellationTokenSource.Token);

		await _channel.Writer.WriteAsync(
			command,
			cts.Token).ConfigureAwait(false);
	}

	public void ExtendExpiry()
	{
		_ = Interlocked.Exchange(ref _expiry, options.Value.Expiry);
	}

	public ValueTask DisposeAsync()
	{
		Dispose(true);
		GC.SuppressFinalize(this);

		return ValueTask.CompletedTask;
	}

	public void Dispose()
	{
		logger.LogInformation("Key:{key} 已經被釋放", Key);
		// 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_timer?.Dispose();
				_timer = null;

				Disposed?.Invoke(this, EventArgs.Empty);
				_channel.Writer.Complete();
				_cancellationTokenSource.Cancel();
			}

			_cancellationTokenSource.Dispose();
			_disposedValue = true;
		}
	}

	private async Task ProcessCommandLoopAsync(CancellationToken cancellationToken = default)
	{
		await Task.Yield();

		var reader = _channel.Reader;
		while (await reader
			.WaitToReadAsync(cancellationToken)
			.ConfigureAwait(false))
		{
			if (reader.TryRead(out var command))
			{
				using var activity = _ActivitySource.StartActivity(
					typeof(MultipleChannel).FullName!,
					 ActivityKind.Internal,
					parentContext: default);
				try
				{
					logger.LogInformation("{logInformation}",
						JsonSerializer.Serialize(new
						{
							LogAt = DateTime.Now.ToString("u"),
							Command = command
						}));

					await Task.Delay(
						TimeSpan.FromSeconds(5),
						cancellationToken)
						.ConfigureAwait(false);

					logger.LogInformation("{logInformation}",
						JsonSerializer.Serialize(new
						{
							LogAt = DateTime.Now.ToString("u"),
							Message = "The task is completed."
						}));
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "{logError}",
						JsonSerializer.Serialize(new
						{
							LogAt = DateTime.Now.ToString("u"),
							Command = command
						}));
				}
			}
		}
	}
}