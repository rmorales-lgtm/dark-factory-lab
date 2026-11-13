using System.Text.Json;

namespace Chronos.Web.Domain;

public sealed record EmployeeInfo(string Id, string Name, string Team);
public sealed record WeekInfo(string Start, string End, int ExpectedHours, int DailyBreakMinutes);
public sealed record WorkItemInfo(int Id, string Title, string Type);
public sealed record MeetingInfo(string Id, string Date, string Start, string End, string Title, string Source, string? TrainingCode = null, bool Cancelled = false);
public sealed record CommitInfo(string Sha, string Date, string Time, int? WorkItemId, string Message);
public sealed record PullRequestInfo(int Id, int WorkItemId, string Title, string? Created, string? Merged);
public sealed record AbsenceInfo(string Date, string Type, double Hours, string Period);

public sealed record WeekFile(
    EmployeeInfo Employee,
    WeekInfo Week,
    IReadOnlyList<WorkItemInfo> WorkItems,
    IReadOnlyList<MeetingInfo> Meetings,
    IReadOnlyList<CommitInfo> Commits,
    IReadOnlyList<PullRequestInfo> PullRequests,
    IReadOnlyList<AbsenceInfo> Absences)
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public DateOnly StartDate => DateOnly.ParseExact(Week.Start, "yyyy-MM-dd");
    public DateOnly EndDate => DateOnly.ParseExact(Week.End, "yyyy-MM-dd");

    /// <summary>Reads a week file with read-only access; never writes.</summary>
    public static WeekFile Load(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Parse(stream);
    }

    public static WeekFile Parse(Stream stream)
    {
        var dto = JsonSerializer.Deserialize<Dto>(stream, Options)
            ?? throw new InvalidDataException("Empty week file.");
        return new WeekFile(
            dto.Employee!, dto.Week!,
            dto.WorkItems ?? [], dto.Meetings ?? [], dto.Commits ?? [],
            dto.PullRequests ?? [], dto.Absences ?? []);
    }

    private sealed class Dto
    {
        public EmployeeInfo? Employee { get; set; }
        public WeekInfo? Week { get; set; }
        public List<WorkItemInfo>? WorkItems { get; set; }
        public List<MeetingInfo>? Meetings { get; set; }
        public List<CommitInfo>? Commits { get; set; }
        public List<PullRequestInfo>? PullRequests { get; set; }
        public List<AbsenceInfo>? Absences { get; set; }
    }
}
