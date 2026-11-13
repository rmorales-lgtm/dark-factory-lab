using Chronos.Web.Domain;

namespace Chronos.Tests.Domain;

public class WeekValidatorTests
{
    private static readonly DateOnly Mon = new(2026, 11, 16);

    private static WeekFile Synthetic(params MeetingInfo[] meetings) => new(
        new EmployeeInfo("E", "Test", "T"),
        new WeekInfo("2026-11-16", "2026-11-20", 40, 20),
        [], meetings, [], [], []);

    private static TimeEntry E(string start, string end, string? cat, SourceKind kind = SourceKind.WorkItem, string id = "1", int day = 0)
    {
        var d = Mon.AddDays(day);
        var s = TimeOnly.Parse(start);
        return new TimeEntry(TimeEntry.MakeKey(d, s, id), d, s, TimeOnly.Parse(end), cat, kind, id, "t");
    }

    private static List<string> ValidateFile(int week, Func<IReadOnlyList<TimeEntry>, IReadOnlyList<TimeEntry>>? change = null)
    {
        var (file, proposal, _) = DomainFixtures.Build(week);
        var entries = change is null ? proposal.Entries : change(proposal.Entries);
        return WeekValidator.Validate(file, entries).ToList();
    }

    [Fact]
    public void T2_AC6_Overlap_SyntheticDetectedWithLabels()
    {
        var entries = new[]
        {
            E("09:00", "10:00", "MGMT", SourceKind.Meeting, "M1"),
            E("09:30", "10:30", "PROJ", SourceKind.WorkItem, "1201"),
            E("09:45", "10:15", "ADMIN", SourceKind.Manual, "x"),
        };
        var v = WeekValidator.Validate(Synthetic(), entries);
        Assert.Contains("Overlap: M1 and 1201 on 2026-11-16", v);
        Assert.Contains("Overlap: M1 and Manual entry on 2026-11-16", v);
        Assert.Contains("Overlap: 1201 and Manual entry on 2026-11-16", v);
    }

    [Fact]
    public void T2_AC6_Overlap_AbsenceIncluded()
    {
        var entries = new[]
        {
            E("08:00", "12:00", "ABSENCE", SourceKind.Absence, "Vacation"),
            E("11:00", "12:30", "PROJ", SourceKind.WorkItem, "1201"),
        };
        Assert.Contains("Overlap: Vacation and 1201 on 2026-11-16", WeekValidator.Validate(Synthetic(), entries));
    }

    [Fact]
    public void T2_AC6_Overlap_TouchingEntriesAreNotOverlaps()
    {
        var entries = new[]
        {
            E("09:00", "10:00", "PROJ", SourceKind.WorkItem, "1201"),
            E("10:00", "11:00", "MGMT", SourceKind.Meeting, "M1"),
        };
        Assert.DoesNotContain(WeekValidator.Validate(Synthetic(), entries), x => x.StartsWith("Overlap"));
    }

    [Fact]
    public void T2_AC6_Overlap_Week1AndWeek2Messages()
    {
        Assert.Contains("Overlap: M4 and M5 on 2026-11-17", ValidateFile(1));
        Assert.Contains("Overlap: M17 and M18 on 2026-11-25", ValidateFile(2));
    }

    [Fact]
    public void T2_AC6_Break_SyntheticNoGapIsViolation()
    {
        var entries = new[]
        {
            E("08:00", "12:00", "PROJ", SourceKind.WorkItem, "1201"),
            E("12:10", "16:00", "PROJ", SourceKind.WorkItem, "1201"),
        };
        Assert.Contains("Break: no 20-minute break on 2026-11-16", WeekValidator.Validate(Synthetic(), entries));
    }

    [Fact]
    public void T2_AC6_Break_ExactlyTwentyMinutesIsEnough()
    {
        var entries = new[]
        {
            E("08:00", "12:00", "PROJ", SourceKind.WorkItem, "1201"),
            E("12:20", "16:00", "PROJ", SourceKind.WorkItem, "1201"),
        };
        Assert.DoesNotContain(WeekValidator.Validate(Synthetic(), entries), x => x.StartsWith("Break"));
    }

    [Fact]
    public void T2_AC6_Break_FullDayAbsenceIsNotWorkedDay()
    {
        var entries = new[]
        {
            E("08:00", "12:00", "ABSENCE", SourceKind.Absence, "Vacation"),
            E("12:00", "16:00", "ABSENCE", SourceKind.Absence, "Vacation"),
        };
        Assert.DoesNotContain(WeekValidator.Validate(Synthetic(), entries), x => x.StartsWith("Break"));
    }

    [Fact]
    public void T2_AC6_Break_OnlyWeek2Monday()
    {
        Assert.Equal(["Break: no 20-minute break on 2026-11-23"], ValidateFile(2).Where(x => x.StartsWith("Break")));
        Assert.DoesNotContain(ValidateFile(1), x => x.StartsWith("Break"));
    }

    [Fact]
    public void T2_AC6_Target_Messages()
    {
        Assert.Contains("Total 36:40 does not match target 36:00", ValidateFile(1));
        Assert.Contains("Total 33:15 does not match target 32:00", ValidateFile(2));
    }

    [Fact]
    public void T2_AC6_Target_SyntheticMatchHasNoTargetViolation()
    {
        // 5 days x 8h = 40h target.
        var entries = Enumerable.Range(0, 5)
            .SelectMany(d => new[] { E("08:00", "12:00", "PROJ", day: d), E("12:30", "16:30", "PROJ", day: d) })
            .ToList();
        Assert.Empty(WeekValidator.Validate(Synthetic(), entries));
    }

    [Fact]
    public void T2_AC6_TrainingCode_M8ChangedToMgmt()
    {
        var v = ValidateFile(1, es => es.Select(e => e.SourceId == "M8" ? e with { Category = "MGMT" } : e).ToList());
        Assert.Contains("Training must use code 5843: M8 on 2026-11-19", v);
    }

    [Fact]
    public void T2_AC6_TrainingCode_UnchangedProposalHasNone()
    {
        Assert.DoesNotContain(ValidateFile(1), x => x.StartsWith("Training"));
        Assert.DoesNotContain(ValidateFile(2), x => x.StartsWith("Training"));
    }

    [Fact]
    public void T2_AC6_TrainingCode_Synthetic()
    {
        var file = Synthetic(new MeetingInfo("MX", "2026-11-16", "09:00", "10:00", "Course", "Calendar", "5843"));
        var entries = new[] { E("09:00", "10:00", "ADMIN", SourceKind.Meeting, "MX") };
        Assert.Contains("Training must use code 5843: MX on 2026-11-16", WeekValidator.Validate(file, entries));
    }

    [Fact]
    public void T2_AC6_MissingCategory()
    {
        var entries = new[] { E("13:00", "14:00", null, SourceKind.Manual, "x") };
        Assert.Contains("Missing category: 13:00-14:00 on 2026-11-16", WeekValidator.Validate(Synthetic(), entries));
    }

    [Fact]
    public void T2_AC6_MissingCategory_NoneInProvidedData()
    {
        Assert.DoesNotContain(ValidateFile(1), x => x.StartsWith("Missing"));
        Assert.DoesNotContain(ValidateFile(2), x => x.StartsWith("Missing"));
    }

    [Fact]
    public void T2_AC4_ProposalViolationCounts_Week1Is2_Week2Is3()
    {
        Assert.Equal(2, ValidateFile(1).Count);
        Assert.Equal(3, ValidateFile(2).Count);
    }

    [Fact]
    public void T2_AC6_ViolationOrder_OverlapBreakTarget()
    {
        Assert.Equal(
            ["Overlap: M17 and M18 on 2026-11-25", "Break: no 20-minute break on 2026-11-23", "Total 33:15 does not match target 32:00"],
            ValidateFile(2));
    }

    [Fact]
    public void T2_AC5_ValidScenarioDomain()
    {
        var (file, proposal, _) = DomainFixtures.Build(1);
        var entries = proposal.Entries.Select(e =>
            e.SourceId == "M5" ? e with { Start = new TimeOnly(15, 0), End = new TimeOnly(15, 30) }
            : e.Date == new DateOnly(2026, 11, 20) && e.Start == new TimeOnly(12, 20) && e.End == new TimeOnly(15, 0)
                ? e with { Start = new TimeOnly(12, 30) }
            : e).ToList();

        var kpis = KpiCalculator.Calculate(file, entries);

        Assert.Equal("36:00", TimeFormat.Hmm(kpis.WorkedMinutes));
        Assert.Equal("36:00", TimeFormat.Hmm(kpis.TargetMinutes));
        Assert.Empty(WeekValidator.Validate(file, entries));
    }
}
