using ChannelSample.AppHost.Channels;
using ChannelSample.AppHost.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ChannelSample.AppHost.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MultipleChannelController(
	TimeProvider timeProvider) : ControllerBase
{
	[HttpPost("First")]
	public Task FirstPushCommandAsync(
	[FromServices] IMultipleDispatcher dispatcher,
	[FromBody] MessageRequest request)
	=> Task.WhenAll(
		dispatcher.PushCommandAsync(
			command: new MultipleCommand(
				Application: nameof(FirstPushCommandAsync),
				MessageAt: timeProvider.GetUtcNow(),
				Message: request.Message),
			cancellationToken: HttpContext.RequestAborted).AsTask(),
		 Task.Run(() => dispatcher.ExtendExpiry(nameof(FirstPushCommandAsync))));

	[HttpPost("Second")]
	public Task SecondPushCommandAsync(
	[FromServices] IMultipleDispatcher dispatcher,
	[FromBody] MessageRequest request)
	=> Task.WhenAll(
		dispatcher.PushCommandAsync(
			command: new MultipleCommand(
				Application: nameof(SecondPushCommandAsync),
				MessageAt: timeProvider.GetUtcNow(),
				Message: request.Message),
			cancellationToken: HttpContext.RequestAborted).AsTask(),
		 Task.Run(() => dispatcher.ExtendExpiry(nameof(SecondPushCommandAsync))));
}