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

    private PeriodicTimer _timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
    private DateTime _lastChecked = DateTime.Now.Subtract(TimeSpan.FromDays(3));

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
        var sources = await Task.WhenAll(_providers.Select(p => p.FetchArticles()));

        var newArticleSincePrevPoll = sources
            .SelectMany(source => source.Where(article => article.PublishedAt > _lastChecked))
            .OrderBy(a => a.PublishedAt);

        foreach (var article in newArticleSincePrevPoll)
            await PublishArticle(article);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken))
        {
            await PollArticles();
            _lastChecked = DateTime.Now;
        }
    }

    public override void Dispose()
    {
        foreach (var provider in _providers)
            provider.Dispose();
        _timer.Dispose();
    }
}