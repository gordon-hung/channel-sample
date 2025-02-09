using ChannelSample.AppHost.Models;
using ChannelSample.AppHost.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ChannelSample.AppHost.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MultipleChannelController(
	TimeProvider timeProvider,
	ISequenceGeneratorUtil sequenceGeneratorUtil) : ControllerBase
{
	[HttpPost("First")]
	public async Task FirstPushCommandAsync(
	[FromServices] IMultipleDispatcher dispatcher,
	[FromBody] MessageRequest request)
	{
		var application = nameof(FirstPushCommandAsync);
		var Sequence = await sequenceGeneratorUtil.GetNextIdForKey(
			key: application,
			cancellationToken: HttpContext.RequestAborted)
			.ConfigureAwait(false);

		await Task.WhenAll(
			dispatcher.PushCommandAsync(
				command: new ChannelCommand(
					Application: application,
					MessageAt: timeProvider.GetUtcNow(),
					Message: request.Message,
					Sequence: Sequence),
				cancellationToken: HttpContext.RequestAborted).AsTask(),
			Task.Run(() => dispatcher.ExtendExpiry(application)))
			.ConfigureAwait(false);
	}

	[HttpPost("Second")]
	public async Task SecondPushCommandAsync(
	[FromServices] IMultipleDispatcher dispatcher,
	[FromBody] MessageRequest request)
	{
		var application = nameof(SecondPushCommandAsync);
		var Sequence = await sequenceGeneratorUtil.GetNextIdForKey(
			key: application,
			cancellationToken: HttpContext.RequestAborted)
			.ConfigureAwait(false);

		await Task.WhenAll(
			dispatcher.PushCommandAsync(
				command: new ChannelCommand(
					Application: application,
					MessageAt: timeProvider.GetUtcNow(),
					Message: request.Message,
					Sequence: Sequence),
				cancellationToken: HttpContext.RequestAborted).AsTask(),
			Task.Run(() => dispatcher.ExtendExpiry(application)))
			.ConfigureAwait(false);
	}
}