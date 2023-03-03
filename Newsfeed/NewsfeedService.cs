using System.ComponentModel.DataAnnotations;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace Corner.Failsafe.Newsfeed;

public interface INewsProvider : IDisposable
{
    public Task<IList<NewsArticle>> FetchArticles();
}

public class NewsfeedOptions
{
    [Range(1, ulong.MaxValue)]
    public ulong ChannelID { get; set; }
    [Required]
    public string BungieApiKey { get; set; } = String.Empty;
    [Required]
    public string YouTubeApiKey { get; set; } = String.Empty;
}

public class NewsfeedService : BackgroundService, IDisposable
{
    private readonly DiscordSocketClient _client;
    private readonly NewsfeedOptions _options;
    private readonly List<INewsProvider> _providers;
    private readonly ILogger<NewsfeedService> _logger;

    private PeriodicTimer _timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
    private DateTime _lastTimeNewDetected = DateTime.Now;

    public NewsfeedService(
        DiscordSocketClient client,
        IOptions<NewsfeedOptions> options,
        ILogger<NewsfeedService> logger
    )
    {
        _client = client;
        _options = options.Value;
        _providers = new List<INewsProvider> {
            new BungieNewsProvider(_options.BungieApiKey),
            new YouTubeNewsSource(_options.YouTubeApiKey),
        };
        _logger = logger;
    }

    private async Task PublishArticle(NewsArticle article)
    {
        var embedBuilder = new EmbedBuilder()
            .WithTitle(article.Title)
            .WithDescription(article.Description)
            .WithImageUrl(article.ImageURL)
            .WithUrl(article.URL)
            .WithTimestamp(article.PublishedAt);

        var channel = await _client.GetChannelAsync(_options.ChannelID);
        var messageChannel = (ISocketMessageChannel)channel;
        await messageChannel.SendMessageAsync(embed: embedBuilder.Build());
    }

    private async Task PollArticles()
    {
        _logger.LogInformation("Polling for articles....");

        var sources = await Task.WhenAll(_providers.Select(p => p.FetchArticles()));

        var newArticles = sources
            .SelectMany(source => source.Where(article => article.PublishedAt > _lastTimeNewDetected))
            .OrderBy(a => a.PublishedAt)
            .ToList();

        _logger.LogInformation($"Found {newArticles.Count} new articles since last poll");

        foreach (var article in newArticles)
            await PublishArticle(article);

        if (newArticles.Count > 0)
            _lastTimeNewDetected = newArticles.Last().PublishedAt;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken))
            await PollArticles();
    }

    public override void Dispose()
    {
        foreach (var provider in _providers)
            provider.Dispose();
        _timer.Dispose();
    }
}