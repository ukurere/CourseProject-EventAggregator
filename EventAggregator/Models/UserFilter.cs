namespace EventAggregator.API.Models;

public class UserFilter
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int FilterId { get; set; }
    public Filter Filter { get; set; } = null!;
}