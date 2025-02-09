using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;

using ChannelSample.AppHost.Models;

namespace ChannelSample.AppHost.Channels;

public class SingleChannel(
	ILogger<SingleChannel> logger,
	IServiceProvider serviceProvider) : IDisposable, IAsyncDisposable
{
	private static readonly ActivitySource _ActivitySource = new(name: typeof(SingleChannel).FullName!);

	private readonly Channel<ChannelCommand> _channel = Channel.CreateUnbounded<ChannelCommand>(
		new UnboundedChannelOptions
		{
			SingleReader = true
		});

	private readonly SemaphoreSlim _locker = new(1, 1);
	private readonly CancellationTokenSource _cancellationTokenSource = new();

	private Task? _backgroundTask;
	private bool _disposedValue;

	public event EventHandler? Disposed;

	~SingleChannel()
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

			_backgroundTask = Task.Run(
				() => ProcessCommandLoopAsync(_cancellationTokenSource.Token),
				_cancellationTokenSource.Token);
		}
		finally
		{
			_ = _locker.Release();
		}
	}

	public async ValueTask PushCommandAsync(ChannelCommand command, CancellationToken cancellationToken = default)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(
			token1: cancellationToken,
			token2: _cancellationTokenSource.Token);

		await _channel.Writer.WriteAsync(
			item: command,
			cancellationToken: cts.Token).ConfigureAwait(false);
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

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
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
					typeof(SingleChannel).FullName!,
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
						TimeSpan.FromSeconds(1),
						cancellationToken)
						.ConfigureAwait(false);

					using (var scope = serviceProvider.CreateScope())
					{
						var channelRepo = scope.ServiceProvider.GetRequiredService<IChannelRepo>();
						await channelRepo.InsertAsync(
							command.Application,
							command.Message,
							command.Sequence,
							command.MessageAt,
							cancellationToken)
							.ConfigureAwait(false);
					}

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
