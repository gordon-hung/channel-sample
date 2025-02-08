using ChannelSample.AppHost.Channels;
using ChannelSample.AppHost.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ChannelSample.AppHost.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SingleChannelController(
	TimeProvider timeProvider) : ControllerBase
{
	[HttpPost]
	public ValueTask PushCommandAsync(
		[FromServices] SingleChannel handler,
		[FromBody] MessageRequest request)
		=> handler.PushCommandAsync(
			request: new SingleCommand(
				MessageAt: timeProvider.GetUtcNow(),
				Message: request.Message),
			cancellationToken: HttpContext.RequestAborted);
}