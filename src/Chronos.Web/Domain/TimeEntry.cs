namespace Chronos.Web.Domain;

public enum SourceKind { Meeting, WorkItem, Absence, Manual }

public sealed record TimeEntry(
    string Key,
    DateOnly Date,
    TimeOnly Start,
    TimeOnly End,
    string? Category,
    SourceKind SourceKind,
    string SourceId,
    string SourceTitle)
{
    public int Minutes => (int)(End - Start).TotalMinutes;

    public static string MakeKey(DateOnly date, TimeOnly start, string sourceId)
        => $"{date:yyyy-MM-dd}T{start:HH:mm}-{sourceId}";
}

public sealed record IgnoredCommit(string Sha, string Date, string Message, string Reason);

public sealed record WeekProposal(IReadOnlyList<TimeEntry> Entries, IReadOnlyList<IgnoredCommit> IgnoredCommits);
