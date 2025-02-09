using System.ComponentModel.DataAnnotations;

namespace ChannelSample.AppHost.ViewModels;

public record MessageRequest
{
    [Required]
    public string Message { get; init; } = default!;
}
