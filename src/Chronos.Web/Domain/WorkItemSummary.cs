namespace Chronos.Web.Domain;

public sealed record PullRequestSummary(int Id, string Title, string State);

public sealed record WorkItemSummary(
    int Id, string Title, string Type, int ProjMinutes, IReadOnlyList<PullRequestSummary> PullRequests)
{
    public static IReadOnlyList<WorkItemSummary> Build(WeekFile week, IReadOnlyList<TimeEntry> entries)
    {
        var result = new List<WorkItemSummary>();
        foreach (var wi in week.WorkItems.OrderBy(w => w.Id))
        {
            var minutes = entries
                .Where(e => e.Category == "PROJ" && e.SourceKind == SourceKind.WorkItem && e.SourceId == wi.Id.ToString())
                .Sum(e => e.Minutes);
            if (minutes <= 0) continue;
            var prs = week.PullRequests
                .Where(p => p.WorkItemId == wi.Id)
                .GroupBy(p => p.Id).Select(g => g.First())
                .OrderBy(p => p.Id)
                .Select(p => new PullRequestSummary(p.Id, p.Title, p.Merged is null ? "open" : "merged"))
                .ToList();
            result.Add(new WorkItemSummary(wi.Id, wi.Title, wi.Type, minutes, prs));
        }
        return result;
    }
}
