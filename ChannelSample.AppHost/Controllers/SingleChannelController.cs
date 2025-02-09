using ChannelSample.AppHost.Channels;
using ChannelSample.AppHost.Models;
using ChannelSample.AppHost.ViewModels;

using Microsoft.AspNetCore.Mvc;

namespace ChannelSample.AppHost.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SingleChannelController(
	TimeProvider timeProvider,
	ISequenceGeneratorUtil sequenceGeneratorUtil) : ControllerBase
{
	[HttpPost]
	public async ValueTask PushCommandAsync(
		[FromServices] SingleChannel channel,
		[FromBody] MessageRequest request)
	{
		var application = nameof(PushCommandAsync);
		var Sequence = await sequenceGeneratorUtil.GetNextIdForKey(
			key: application,
			cancellationToken: HttpContext.RequestAborted)
			.ConfigureAwait(false);

		await channel.PushCommandAsync(
			command: new ChannelCommand(
				Application: application,
				MessageAt: timeProvider.GetUtcNow(),
				Message: request.Message,
				Sequence: Sequence),
			cancellationToken: HttpContext.RequestAborted)
			.ConfigureAwait(false);
	}
}
