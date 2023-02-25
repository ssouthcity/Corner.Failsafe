using Google.Apis.Services;
using Google.Apis.YouTube.v3;

namespace Corner.Failsafe.Newsfeed;

public class YouTubeNewsSource : INewsProvider
{
    private readonly YouTubeService _youTube;

    public YouTubeNewsSource(string apiKey)
    {
        _youTube = new YouTubeService(new BaseClientService.Initializer
        {
            ApplicationName = "Failsafe News Source",
            ApiKey = apiKey,
        });
    }

    public async Task<IList<NewsArticle>> FetchArticles()
    {
        var request = _youTube.PlaylistItems.List("snippet");
        request.PlaylistId = "PLw2gyMFmq40pL-jC1jFPreWHGuV7g_Kmu"; // playlist with english Destiny 2 videos

        var response = await request.ExecuteAsync();

        var articles = response.Items
            .Select(video => new NewsArticle
            {
                Title = video.Snippet.Title,
                Description = video.Snippet.Description.Split("\n").First(),
                URL = $"https://www.youtube.com/watch?v={video.Snippet.ResourceId.VideoId}",
                ImageURL = video.Snippet.Thumbnails.Standard.Url,
                PublishedAt = video.Snippet.PublishedAt.GetValueOrDefault()
            })
            .ToList();

        return articles;
    }

    public void Dispose()
    {
        _youTube.Dispose();
    }
}