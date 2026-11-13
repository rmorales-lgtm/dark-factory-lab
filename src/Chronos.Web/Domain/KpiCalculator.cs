namespace Chronos.Web.Domain;

public sealed record WeekKpis(
    int WorkedMinutes,
    int TargetMinutes,
    IReadOnlyDictionary<string, int> MinutesByCategory,
    int ProjectSharePercent,
    int MeetingMinutes,
    IReadOnlyDictionary<DateOnly, int> WorkedByDay);

public static class KpiCalculator
{
    public static readonly string[] CategoryOrder = ["PROJ", "MGMT", "5843", "ADMIN", "ABSENCE"];

    public static WeekKpis Calculate(WeekFile week, IReadOnlyList<TimeEntry> entries)
    {
        var worked = entries.Where(e => e.Category != "ABSENCE").Sum(e => e.Minutes);
        var target = week.Week.ExpectedHours * 60 - (int)Math.Round(week.Absences.Sum(a => a.Hours) * 60);

        var byCat = CategoryOrder.ToDictionary(c => c, c => entries.Where(e => e.Category == c).Sum(e => e.Minutes));
        var proj = byCat["PROJ"];
        var share = worked == 0 ? 0 : (int)Math.Round(proj * 100.0 / worked, MidpointRounding.AwayFromZero);
        var meeting = byCat["MGMT"] + byCat["ADMIN"] + byCat["5843"];

        var byDay = new SortedDictionary<DateOnly, int>();
        for (var d = week.StartDate; d <= week.EndDate; d = d.AddDays(1))
            if (d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                byDay[d] = entries.Where(e => e.Date == d && e.Category != "ABSENCE").Sum(e => e.Minutes);

        return new WeekKpis(worked, target, byCat, share, meeting, byDay);
    }
}
