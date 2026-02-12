namespace EventAggregator.API.Models;

public class Event
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ContentHtml { get; set; } 

    public string SourceUrl { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Author { get; set; }

    public DateTime PublishedDate { get; set; }
    public DateTime? EventDate { get; set; }

    public string? Location { get; set; }
    public string? Category { get; set; }

    public int? MaxPeople { get; set; }

    public ICollection<UserEventStatus> UserEventStatuses { get; set; } = new List<UserEventStatus>();
}