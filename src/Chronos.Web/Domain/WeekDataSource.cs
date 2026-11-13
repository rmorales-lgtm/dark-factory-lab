using Chronos.Web.Data;

namespace Chronos.Web.Domain;

public interface IWeekDataSource
{
    /// <summary>Week start dates (yyyy-MM-dd) that can be shown, in order.</summary>
    IReadOnlyList<string> WeekStarts { get; }

    /// <summary>Returns the week starting on the given date, or null when unknown.</summary>
    WeekFile? Get(string? weekStart);
}

/// <summary>Reads week1.json and week2.json from the data directory (read-only).</summary>
public sealed class FileWeekDataSource : IWeekDataSource
{
    private readonly Dictionary<string, WeekFile> _weeks = new(StringComparer.Ordinal);

    public FileWeekDataSource(ChronosPaths paths)
    {
        foreach (var name in new[] { "week1.json", "week2.json" })
        {
            var file = WeekFile.Load(Path.Combine(paths.DataDirectory, name));
            _weeks[file.Week.Start] = file;
        }
        WeekStarts = _weeks.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
    }

    public IReadOnlyList<string> WeekStarts { get; }

    public WeekFile? Get(string? weekStart)
        => weekStart is not null && _weeks.TryGetValue(weekStart, out var w) ? w : null;
}
