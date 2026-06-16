using System.ComponentModel.DataAnnotations;

namespace EventAggregator.API.Models;

public class UserKeyword
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Keyword { get; set; } = string.Empty;
}
