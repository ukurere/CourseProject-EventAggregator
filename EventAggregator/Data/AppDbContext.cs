using Microsoft.EntityFrameworkCore;
using EventAggregator.API.Models;

namespace EventAggregator.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Filter> Filters => Set<Filter>();
    public DbSet<UserFilter> UserFilters => Set<UserFilter>();
    public DbSet<UserEventStatus> UserEventStatuses => Set<UserEventStatus>();
    public DbSet<FeedSource> FeedSources => Set<FeedSource>();
    public DbSet<UserKeyword> UserKeywords => Set<UserKeyword>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserFilter>()
            .HasKey(uf => new { uf.UserId, uf.FilterId });

        modelBuilder.Entity<UserEventStatus>()
            .HasOne(ues => ues.User)
            .WithMany(u => u.UserEventStatuses)
            .HasForeignKey(ues => ues.UserId);

        modelBuilder.Entity<UserEventStatus>()
            .HasOne(ues => ues.Event)
            .WithMany(e => e.UserEventStatuses)
            .HasForeignKey(ues => ues.EventId);

        modelBuilder.Entity<Event>()
            .HasOne(e => e.FeedSource)
            .WithMany(fs => fs.Events)
            .HasForeignKey(e => e.FeedSourceId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}