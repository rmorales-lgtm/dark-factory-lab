namespace Chronos.Web.Domain;

public static class ProposalBuilder
{
    private static readonly TimeOnly DayStart = new(8, 0);
    private static readonly TimeOnly BreakStart = new(12, 0);
    private static readonly TimeOnly BreakEnd = new(12, 20);
    private static readonly TimeOnly DayEnd = new(16, 20);

    public static WeekProposal Build(WeekFile week)
    {
        var entries = new List<TimeEntry>();
        var start = week.StartDate;
        var end = week.EndDate;

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            var ds = date.ToString("yyyy-MM-dd");
            var blocked = new List<(int s, int e)>();
            var dayEntries = new List<TimeEntry>();

            foreach (var m in week.Meetings.Where(m => m.Date == ds && !m.Cancelled))
            {
                var s = TimeOnly.Parse(m.Start);
                var e = TimeOnly.Parse(m.End);
                var cat = !string.IsNullOrEmpty(m.TrainingCode) ? m.TrainingCode
                    : m.Source == "Calendar" ? "ADMIN" : "MGMT";
                dayEntries.Add(new TimeEntry(TimeEntry.MakeKey(date, s, m.Id), date, s, e, cat,
                    SourceKind.Meeting, m.Id, m.Title));
                blocked.Add((Min(s), Min(e)));
            }

            foreach (var a in week.Absences.Where(a => a.Date == ds))
            {
                var periods = a.Period.ToLowerInvariant() switch
                {
                    "morning" => new[] { (DayStart, BreakStart) },
                    "afternoon" => new[] { (BreakEnd, DayEnd) },
                    _ => new[] { (DayStart, BreakStart), (BreakEnd, DayEnd) },
                };
                foreach (var (s, e) in periods)
                {
                    dayEntries.Add(new TimeEntry(TimeEntry.MakeKey(date, s, a.Type), date, s, e, "ABSENCE",
                        SourceKind.Absence, a.Type, a.Type));
                    blocked.Add((Min(s), Min(e)));
                }
            }

            var workItemId = week.Commits
                .Where(c => c.Date == ds && c.WorkItemId.HasValue)
                .OrderBy(c => TimeOnly.Parse(c.Time))
                .Select(c => c.WorkItemId)
                .FirstOrDefault();
            var title = workItemId.HasValue
                ? week.WorkItems.FirstOrDefault(w => w.Id == workItemId)?.Title ?? ""
                : "";

            foreach (var (fs, fe) in FreeSlots(blocked))
            {
                var s = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(fs));
                var e = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(fe));
                var id = workItemId?.ToString() ?? "none";
                dayEntries.Add(new TimeEntry(TimeEntry.MakeKey(date, s, id), date, s, e,
                    workItemId.HasValue ? "PROJ" : null, SourceKind.WorkItem, id, title));
            }

            entries.AddRange(dayEntries.OrderBy(x => x.Start).ThenBy(x => x.End).ThenBy(x => x.SourceId, StringComparer.Ordinal));
        }

        var ignored = new List<IgnoredCommit>();
        foreach (var c in week.Commits.OrderBy(c => c.Date, StringComparer.Ordinal).ThenBy(c => c.Time, StringComparer.Ordinal))
        {
            var d = DateOnly.ParseExact(c.Date, "yyyy-MM-dd");
            if (!c.WorkItemId.HasValue)
                ignored.Add(new IgnoredCommit(c.Sha, c.Date, c.Message, "no work item"));
            else if (d < start || d > end)
                ignored.Add(new IgnoredCommit(c.Sha, c.Date, c.Message, "outside the week"));
        }

        return new WeekProposal(entries, ignored);
    }

    private static int Min(TimeOnly t) => t.Hour * 60 + t.Minute;

    private static IEnumerable<(int, int)> FreeSlots(List<(int s, int e)> blocked)
    {
        var segments = new[] { (Min(DayStart), Min(BreakStart)), (Min(BreakEnd), Min(DayEnd)) };
        foreach (var (segS, segE) in segments)
        {
            var cursor = segS;
            foreach (var (bs, be) in blocked.OrderBy(b => b.s))
            {
                if (be <= cursor || bs >= segE) continue;
                if (bs > cursor) yield return (cursor, bs);
                cursor = Math.Max(cursor, be);
            }
            if (cursor < segE) yield return (cursor, segE);
        }
    }
}
