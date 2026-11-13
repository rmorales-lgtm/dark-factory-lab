using Chronos.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Web.Data;

public class ChronosDbContext : DbContext
{
    public ChronosDbContext(DbContextOptions<ChronosDbContext> options) : base(options)
    {
    }

    public DbSet<SavedEntry> SavedEntries => Set<SavedEntry>();
    public DbSet<WeekState> WeekStates => Set<WeekState>();
    public DbSet<HistoryItem> HistoryItems => Set<HistoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SavedEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.WeekStart, x.Key }).IsUnique();
            e.Property(x => x.SourceKind).HasConversion<string>();
        });
        modelBuilder.Entity<WeekState>(e => e.HasKey(x => x.WeekStart));
        modelBuilder.Entity<HistoryItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.WeekStart, x.Order });
        });
    }
}

public sealed record ChronosPaths(string DataDirectory);

/// <summary>An entry saved for one week (times and dates stored as yyyy-MM-dd and HH:mm text).</summary>
public class SavedEntry
{
    public int Id { get; set; }
    public string WeekStart { get; set; } = "";
    public string Key { get; set; } = "";
    public string Date { get; set; } = "";
    public string Start { get; set; } = "";
    public string End { get; set; } = "";
    public string? Category { get; set; }
    public SourceKind SourceKind { get; set; }
    public string SourceId { get; set; } = "";
    public string SourceTitle { get; set; } = "";
    public int Sort { get; set; }
}

public class WeekState
{
    public string WeekStart { get; set; } = "";
    public string Status { get; set; } = "Draft";
    public string? ManagerComment { get; set; }
    /// <summary>True once the week has saved changes (even when every entry was deleted).</summary>
    public bool HasSavedChanges { get; set; }
}

public class HistoryItem
{
    public int Id { get; set; }
    public string WeekStart { get; set; } = "";
    public int Order { get; set; }
    public string Text { get; set; } = "";
}
