using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Corner.Failsafe.Newsfeed;

public class BungieNewsProvider : INewsProvider, IDisposable
{
    private HttpClient _client = new();

    public BungieNewsProvider(string apiKey)
        => _client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

    private class ArticleObject
    {
        public string Title { get; set; } = String.Empty;
        public string Description { get; set; } = String.Empty;
        public string Link { get; set; } = String.Empty;
        public string ImagePath { get; set; } = String.Empty;
        public DateTime PubDate { get; set; }
    }

    public async Task<IList<NewsArticle>> FetchArticles()
    {
        using var response = await _client.GetAsync("https://www.bungie.net/Platform/Content/Rss/NewsArticles/0/");
        using var stream = await response.Content.ReadAsStreamAsync();
        using var streamReader = new StreamReader(stream);
        using var jsonReader = new JsonTextReader(streamReader);

        var responseObject = await JObject.ReadFromAsync(jsonReader);
        var articleTokens = responseObject["Response"]!["NewsArticles"]!;

        var articleObjects = JsonConvert.DeserializeObject<List<ArticleObject>>(articleTokens.ToString())!;

        var articles = articleObjects
            .Select(obj => new NewsArticle
            {
                Title = obj.Title,
                Description = obj.Description,
                URL = $"https://bungie.net/{obj.Link}",
                ImageURL = obj.ImagePath,
                PublishedAt = obj.PubDate,
            })
            .ToList();

        return articles;
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}