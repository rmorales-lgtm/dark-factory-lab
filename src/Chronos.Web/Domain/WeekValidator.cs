namespace Chronos.Web.Domain;

public static class WeekValidator
{
    private const int MinBreakMinutes = 20;

    public static IReadOnlyList<string> Validate(WeekFile week, IReadOnlyList<TimeEntry> entries)
    {
        var trainingIds = week.Meetings
            .Where(m => !string.IsNullOrEmpty(m.TrainingCode))
            .Select(m => m.Id)
            .ToHashSet(StringComparer.Ordinal);

        var violations = new List<string>();
        var ordered = entries
            .OrderBy(e => e.Date).ThenBy(e => e.Start).ThenBy(e => e.End).ThenBy(e => e.SourceId, StringComparer.Ordinal)
            .ToList();

        // Overlap (strict: touching entries do not overlap).
        for (var i = 0; i < ordered.Count; i++)
            for (var j = i + 1; j < ordered.Count; j++)
            {
                var a = ordered[i];
                var b = ordered[j];
                if (b.Date != a.Date) break;
                if (b.Start < a.End)
                    violations.Add($"Overlap: {Label(a)} and {Label(b)} on {a.Date:yyyy-MM-dd}");
            }

        // Break.
        foreach (var day in ordered.GroupBy(e => e.Date))
        {
            if (day.All(e => e.Category == "ABSENCE")) continue;
            var list = day.ToList();
            var maxEnd = list[0].End;
            var hasBreak = false;
            for (var i = 1; i < list.Count; i++)
            {
                if ((list[i].Start - maxEnd).TotalMinutes >= MinBreakMinutes) { hasBreak = true; break; }
                if (list[i].End > maxEnd) maxEnd = list[i].End;
            }
            if (!hasBreak)
                violations.Add($"Break: no 20-minute break on {day.Key:yyyy-MM-dd}");
        }

        // Training code.
        foreach (var e in ordered)
            if (e.SourceKind == SourceKind.Meeting && trainingIds.Contains(e.SourceId) && e.Category != "5843")
                violations.Add($"Training must use code 5843: {e.SourceId} on {e.Date:yyyy-MM-dd}");

        // Category.
        foreach (var e in ordered)
            if (string.IsNullOrWhiteSpace(e.Category))
                violations.Add($"Missing category: {e.Start:HH:mm}-{e.End:HH:mm} on {e.Date:yyyy-MM-dd}");

        // Target.
        var kpis = KpiCalculator.Calculate(week, entries);
        if (kpis.WorkedMinutes != kpis.TargetMinutes)
            violations.Add($"Total {TimeFormat.Hmm(kpis.WorkedMinutes)} does not match target {TimeFormat.Hmm(kpis.TargetMinutes)}");

        return violations;
    }

    private static string Label(TimeEntry e) => e.SourceKind == SourceKind.Manual ? "Manual entry" : e.SourceId;
}
