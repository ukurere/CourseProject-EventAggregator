namespace EventAggregator.API.Models;

public class UserEventStatus
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public string Status { get; set; } = "Interesting";

    public bool IsInCalendar { get; set; }
    public DateTime MarkedAt { get; set; } = DateTime.UtcNow;
}