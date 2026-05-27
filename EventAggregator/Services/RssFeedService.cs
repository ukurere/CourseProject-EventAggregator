using System.ServiceModel.Syndication;
using System.Xml;
using EventAggregator.API.Models;
using EventAggregator.Data;
using Microsoft.EntityFrameworkCore;

namespace EventAggregator.API.Services;

public class RssFeedService
{
    private readonly AppDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly ILogger<RssFeedService> _logger;

    public RssFeedService(AppDbContext db, HttpClient httpClient, ILogger<RssFeedService> logger)
    {
        _db = db;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task RefreshAllFeedsAsync()
    {
        var sources = await _db.FeedSources
            .Where(s => s.IsActive)
            .ToListAsync();

        foreach (var source in sources)
        {
            try
            {
                await RefreshFeedAsync(source);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не вдалося оновити стрічку {Name} ({Url})", source.Name, source.FeedUrl);
            }
        }
    }

    public async Task RefreshFeedAsync(FeedSource source)
    {
        var stream = await _httpClient.GetStreamAsync(source.FeedUrl);

        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
        using var reader = XmlReader.Create(stream, settings);
        var feed = SyndicationFeed.Load(reader);

        var existingUrls = (await _db.Events
            .Where(e => e.FeedSourceId == source.Id)
            .Select(e => e.SourceUrl)
            .ToListAsync())
            .ToHashSet();

        var newEvents = new List<Event>();

        foreach (var item in feed.Items)
        {
            var url = item.Links.FirstOrDefault()?.Uri?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(url) || existingUrls.Contains(url))
                continue;

            newEvents.Add(new Event
            {
                Title = item.Title?.Text ?? string.Empty,
                Description = item.Summary?.Text ?? string.Empty,
                SourceUrl = url,
                PublishedDate = item.PublishDate == default
                    ? DateTime.UtcNow
                    : item.PublishDate.UtcDateTime,
                FeedSourceId = source.Id,
                Category = source.Category,
                Author = item.Authors.FirstOrDefault()?.Name,
                ImageUrl = item.Links
                    .FirstOrDefault(l => l.RelationshipType == "enclosure")?.Uri?.ToString(),
            });
        }

        _db.Events.AddRange(newEvents);
        source.LastFetched = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Стрічка {Name}: додано {Count} нових подій", source.Name, newEvents.Count);
    }
}
