using System.ComponentModel.DataAnnotations;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace Corner.Failsafe.Discord;

public class DiscordSocketOptions
{
    [Required(AllowEmptyStrings = false)]
    public string Token { get; set; } = String.Empty;
}

public class DiscordSocketService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly DiscordSocketOptions _options;
    private readonly ILogger<DiscordSocketService> _logger;

    public DiscordSocketService(
        DiscordSocketClient client,
        IOptions<DiscordSocketOptions> options,
        ILogger<DiscordSocketService> logger
    )
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _client.LoginAsync(TokenType.Bot, _options.Token).WaitAsync(cancellationToken);
            await _client.StartAsync().WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Startup aborted, exiting gracefully");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _client.StopAsync().WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogCritical("Discord.NET client could not be gracefully shut down");
        }
    }
}
