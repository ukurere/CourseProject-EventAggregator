using System.ComponentModel.DataAnnotations;

namespace EventAggregator.API.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? PhotoUrl { get; set; }

    public string ReportFrequency { get; set; } = "Weekly";
    public DateTime? LastReportSent { get; set; }

    public string PasswordHash { get; set; } = string.Empty;
    public bool TwoFactorEnabled { get; set; } = false;
    public string? TwoFactorSecret { get; set; }

    public ICollection<UserFilter> UserFilters { get; set; } = new List<UserFilter>();
    public ICollection<UserEventStatus> UserEventStatuses { get; set; } = new List<UserEventStatus>();
}