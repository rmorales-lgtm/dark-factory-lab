using System.Globalization;
using Chronos.Web.Data;
using Chronos.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Web.Services;

/// <summary>A refusal to change a locked week (HTTP status and plain text message).</summary>
public sealed record EditBlock(int StatusCode, string Message);

/// <summary>Reads and changes the entries of a week. Saved entries replace the proposal once a change is made.</summary>
public sealed class WeekService
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string TimeFormat = "HH:mm";
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private readonly ChronosDbContext _db;

    public WeekService(ChronosDbContext db)
    {
        _db = db;
    }

    public string GetStatus(string weekStart)
        => _db.WeekStates.AsNoTracking().Where(s => s.WeekStart == weekStart).Select(s => s.Status).FirstOrDefault() ?? "Draft";

    /// <summary>
    /// Single place that decides whether the entries of a week can change.
    /// Only Draft and Rejected weeks can; Ticket 3 relies on this.
    /// </summary>
    public EditBlock? CheckEditable(string weekStart) => GetStatus(weekStart) switch
    {
        "Draft" or "Rejected" => null,
        "Submitted" => new EditBlock(409, "Week is awaiting approval"),
        _ => new EditBlock(409, "Week is locked")
    };

    public IReadOnlyList<TimeEntry> GetEntries(WeekFile week)
    {
        var start = week.Week.Start;
        var saved = _db.WeekStates.AsNoTracking().Any(s => s.WeekStart == start && s.HasSavedChanges);
        if (!saved)
            return ProposalBuilder.Build(week).Entries;

        return _db.SavedEntries.AsNoTracking()
            .Where(e => e.WeekStart == start)
            .OrderBy(e => e.Date).ThenBy(e => e.Start).ThenBy(e => e.Sort)
            .AsEnumerable()
            .Select(ToEntry)
            .ToList();
    }

    public bool Edit(WeekFile week, string key, TimeOnly start, TimeOnly end, string? category)
    {
        Materialize(week);
        var row = _db.SavedEntries.FirstOrDefault(e => e.WeekStart == week.Week.Start && e.Key == key);
        if (row is null) return false;
        row.Start = start.ToString(TimeFormat, Inv);
        row.End = end.ToString(TimeFormat, Inv);
        row.Category = NormalizeCategory(category);
        _db.SaveChanges();
        return true;
    }

    public bool Delete(WeekFile week, string key)
    {
        Materialize(week);
        var row = _db.SavedEntries.FirstOrDefault(e => e.WeekStart == week.Week.Start && e.Key == key);
        if (row is null) return false;
        _db.SavedEntries.Remove(row);
        _db.SaveChanges();
        return true;
    }

    public void Add(WeekFile week, DateOnly date, TimeOnly start, TimeOnly end, string? category)
    {
        Materialize(week);
        var sort = _db.SavedEntries.Where(e => e.WeekStart == week.Week.Start).Select(e => (int?)e.Sort).Max() ?? 0;
        _db.SavedEntries.Add(new SavedEntry
        {
            WeekStart = week.Week.Start,
            Key = "manual-" + Guid.NewGuid().ToString("N"),
            Date = date.ToString(DateFormat, Inv),
            Start = start.ToString(TimeFormat, Inv),
            End = end.ToString(TimeFormat, Inv),
            Category = NormalizeCategory(category),
            SourceKind = SourceKind.Manual,
            SourceId = "",
            SourceTitle = "Manual entry",
            Sort = sort + 1
        });
        _db.SaveChanges();
    }

    /// <summary>Deletes the saved changes of the week; the proposal is shown again.</summary>
    public void Reset(string weekStart)
    {
        _db.SavedEntries.Where(e => e.WeekStart == weekStart).ExecuteDelete();
        var state = _db.WeekStates.FirstOrDefault(s => s.WeekStart == weekStart);
        if (state is not null)
        {
            state.HasSavedChanges = false;
            _db.SaveChanges();
        }
    }

    /// <summary>The first change copies the proposal into saved entries (this week only).</summary>
    private void Materialize(WeekFile week)
    {
        var start = week.Week.Start;
        var state = _db.WeekStates.FirstOrDefault(s => s.WeekStart == start);
        if (state is null)
        {
            state = new WeekState { WeekStart = start };
            _db.WeekStates.Add(state);
        }
        if (state.HasSavedChanges) return;

        var sort = 0;
        foreach (var e in ProposalBuilder.Build(week).Entries)
        {
            _db.SavedEntries.Add(new SavedEntry
            {
                WeekStart = start,
                Key = e.Key,
                Date = e.Date.ToString(DateFormat, Inv),
                Start = e.Start.ToString(TimeFormat, Inv),
                End = e.End.ToString(TimeFormat, Inv),
                Category = e.Category,
                SourceKind = e.SourceKind,
                SourceId = e.SourceId,
                SourceTitle = e.SourceTitle,
                Sort = ++sort
            });
        }
        state.HasSavedChanges = true;
        _db.SaveChanges();
    }

    private static string? NormalizeCategory(string? category)
        => string.IsNullOrWhiteSpace(category) ? null : category.Trim();

    private static TimeEntry ToEntry(SavedEntry e) => new(
        e.Key,
        DateOnly.ParseExact(e.Date, DateFormat, Inv),
        TimeOnly.ParseExact(e.Start, TimeFormat, Inv),
        TimeOnly.ParseExact(e.End, TimeFormat, Inv),
        e.Category, e.SourceKind, e.SourceId, e.SourceTitle);
}
