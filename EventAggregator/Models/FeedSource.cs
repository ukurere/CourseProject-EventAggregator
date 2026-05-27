namespace EventAggregator.API.Models;

public class FeedSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FeedUrl { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Language { get; set; } = "uk";
    public bool IsActive { get; set; } = true;
    public DateTime? LastFetched { get; set; }

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
