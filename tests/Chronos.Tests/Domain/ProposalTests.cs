using Chronos.Web.Domain;

namespace Chronos.Tests.Domain;

internal static class DomainFixtures
{
    public static string DataDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "data")))
            dir = dir.Parent;
        return Path.Combine(dir?.FullName ?? throw new DirectoryNotFoundException("data folder not found"), "data");
    }

    public static WeekFile Load(int week) => WeekFile.Load(Path.Combine(DataDir(), $"week{week}.json"));

    public static (WeekFile File, WeekProposal Proposal, WeekKpis Kpis) Build(int week)
    {
        var file = Load(week);
        var proposal = ProposalBuilder.Build(file);
        return (file, proposal, KpiCalculator.Calculate(file, proposal.Entries));
    }

    public static string Describe(TimeEntry e)
        => $"{e.Start:HH:mm}-{e.End:HH:mm} {e.SourceId} {e.Category ?? "none"}";
}

public class ProposalTests
{
    public static TheoryData<int, string, string, string> Days => new()
    {
        { 1, "2026-11-16", "08:00-09:00 1201 PROJ|09:00-09:15 M1 MGMT|09:15-12:00 1201 PROJ|12:20-13:30 1201 PROJ|13:30-15:00 M2 MGMT|15:00-16:20 1201 PROJ", "8:00" },
        { 1, "2026-11-17", "08:00-09:00 1202 PROJ|09:00-09:15 M3 MGMT|09:15-12:00 1202 PROJ|12:20-14:00 1202 PROJ|14:00-15:00 M4 MGMT|14:30-15:30 M5 MGMT|15:30-16:20 1202 PROJ", "8:30" },
        { 1, "2026-11-18", "08:00-09:00 1203 PROJ|09:00-09:15 M6 MGMT|09:15-12:00 1203 PROJ|12:20-16:20 Sick ABSENCE", "4:00" },
        { 1, "2026-11-19", "08:00-09:00 1203 PROJ|09:00-09:15 M7 MGMT|09:15-10:00 1203 PROJ|10:00-12:00 M8 5843|12:20-16:20 1203 PROJ", "8:00" },
        { 1, "2026-11-20", "08:00-09:00 1203 PROJ|09:00-09:15 M9 MGMT|09:15-12:00 1203 PROJ|12:20-15:00 1203 PROJ|15:00-16:00 M10 MGMT|16:00-16:30 M11 ADMIN", "8:10" },
        { 2, "2026-11-23", "08:00-09:00 1203 PROJ|09:00-09:15 M12 MGMT|09:15-11:30 1203 PROJ|11:30-13:00 M13 MGMT|13:00-16:20 1203 PROJ", "8:20" },
        { 2, "2026-11-24", "08:00-09:00 1204 PROJ|09:00-09:15 M14 MGMT|09:15-12:00 1204 PROJ|12:20-13:00 1204 PROJ|13:00-17:00 M15 5843", "8:40" },
        { 2, "2026-11-25", "08:00-09:00 1205 PROJ|09:00-09:15 M16 MGMT|09:15-10:00 1205 PROJ|10:00-10:30 M17 MGMT|10:15-11:00 M18 MGMT|11:00-12:00 1205 PROJ|12:20-16:20 1205 PROJ", "8:15" },
        { 2, "2026-11-26", "08:00-09:00 1205 PROJ|09:00-09:15 M19 MGMT|09:15-12:00 1205 PROJ|12:20-15:30 1205 PROJ|15:30-16:00 M20 ADMIN|16:00-16:20 1205 PROJ", "8:00" },
        { 2, "2026-11-27", "08:00-12:00 Vacation ABSENCE|12:20-16:20 Vacation ABSENCE", "0:00" },
    };

    [Theory]
    [MemberData(nameof(Days))]
    public void T1_AC3_DayEntriesMatchReferenceProposal(int week, string date, string expected, string worked)
    {
        var (_, proposal, kpis) = DomainFixtures.Build(week);
        var day = DateOnly.Parse(date);

        var actual = proposal.Entries.Where(e => e.Date == day).Select(DomainFixtures.Describe);

        Assert.Equal(expected.Split('|'), actual);
        Assert.Equal(worked, TimeFormat.Hmm(kpis.WorkedByDay[day]));
    }

    [Theory]
    [InlineData(1, 5, 28)]
    [InlineData(2, 5, 25)]
    public void T1_AC3_EntryCountPerWeek(int week, int days, int expectedCount)
    {
        var (_, proposal, _) = DomainFixtures.Build(week);
        Assert.Equal(expectedCount, proposal.Entries.Count);
        Assert.Equal(days, proposal.Entries.Select(e => e.Date).Distinct().Count());
    }

    [Fact]
    public void T1_AC3_KeysAreStableAndUnique()
    {
        foreach (var week in new[] { 1, 2 })
        {
            var (_, first, _) = DomainFixtures.Build(week);
            var (_, second, _) = DomainFixtures.Build(week);
            Assert.Equal(first.Entries.Select(e => e.Key), second.Entries.Select(e => e.Key));
            Assert.Equal(first.Entries.Count, first.Entries.Select(e => e.Key).Distinct().Count());
        }
        var (_, w1, _) = DomainFixtures.Build(1);
        Assert.Equal("2026-11-16T09:00-M1", w1.Entries.First(e => e.SourceId == "M1").Key);
        Assert.Equal("2026-11-16T08:00-1201", w1.Entries.First(e => e.SourceId == "1201").Key);
    }

    [Fact]
    public void T1_AC3_DayWithoutCommitWorkItemHasNoCategory()
    {
        var file = DomainFixtures.Load(1) with { Commits = [] };
        var proposal = ProposalBuilder.Build(file);
        Assert.Contains(proposal.Entries, e => e.SourceKind == SourceKind.WorkItem);
        Assert.All(proposal.Entries.Where(e => e.SourceKind == SourceKind.WorkItem), e => Assert.Null(e.Category));
    }

    [Fact]
    public void T1_AC3_FirstCommitOfDayChoosesWorkItemEvenIfUnsortedInFile()
    {
        var file = DomainFixtures.Load(1);
        var commits = new List<CommitInfo>
        {
            new("late", "2026-11-16", "16:00", 1202, "late"),
            new("early", "2026-11-16", "09:30", 1201, "early"),
        };
        var proposal = ProposalBuilder.Build(file with { Commits = commits });
        Assert.All(proposal.Entries.Where(e => e.Date == new DateOnly(2026, 11, 16) && e.Category == "PROJ"),
            e => Assert.Equal("1201", e.SourceId));
    }

    [Theory]
    [InlineData(1, "PROJ", "28:25")]
    [InlineData(1, "MGMT", "5:45")]
    [InlineData(1, "5843", "2:00")]
    [InlineData(1, "ADMIN", "0:30")]
    [InlineData(1, "ABSENCE", "4:00")]
    [InlineData(2, "PROJ", "25:00")]
    [InlineData(2, "MGMT", "3:45")]
    [InlineData(2, "5843", "4:00")]
    [InlineData(2, "ADMIN", "0:30")]
    [InlineData(2, "ABSENCE", "8:00")]
    public void T1_AC3_CategoryTotals(int week, string category, string expected)
    {
        var (_, _, kpis) = DomainFixtures.Build(week);
        Assert.Equal(expected, TimeFormat.Hmm(kpis.MinutesByCategory[category]));
    }

    [Theory]
    [InlineData(1, "36:40", "36:00")]
    [InlineData(2, "33:15", "32:00")]
    public void T1_AC3_WorkedAndTarget(int week, string worked, string target)
    {
        var (_, _, kpis) = DomainFixtures.Build(week);
        Assert.Equal(worked, TimeFormat.Hmm(kpis.WorkedMinutes));
        Assert.Equal(target, TimeFormat.Hmm(kpis.TargetMinutes));
    }

    [Fact]
    public void T1_AC3_LoaderDoesNotModifyDataFiles()
    {
        var path = Path.Combine(DomainFixtures.DataDir(), "week1.json");
        var before = File.GetLastWriteTimeUtc(path);
        DomainFixtures.Load(1);
        Assert.Equal(before, File.GetLastWriteTimeUtc(path));
    }

    [Theory]
    [InlineData(0, "0:00")]
    [InlineData(30, "0:30")]
    [InlineData(2200, "36:40")]
    [InlineData(1995, "33:15")]
    public void T1_AC3_HmmFormatsMinutes(int minutes, string expected)
        => Assert.Equal(expected, TimeFormat.Hmm(minutes));

    [Fact]
    public void T1_AC4_NoEntryForCancelledMeetingM21()
    {
        var (_, proposal, _) = DomainFixtures.Build(2);
        Assert.DoesNotContain(proposal.Entries, e => e.SourceId == "M21");
    }

    [Fact]
    public void T1_AC4_Week2IgnoredActivityListsBothCommitsWithReason()
    {
        var (_, proposal, _) = DomainFixtures.Build(2);
        var actual = proposal.IgnoredCommits.Select(c => $"{c.Sha}: {c.Reason}").OrderBy(x => x);
        Assert.Equal(new[] { "b9c0d1e: outside the week", "f3a4b5c: no work item" }, actual);
    }

    [Fact]
    public void T1_AC4_Week1HasNoIgnoredActivity()
    {
        var (_, proposal, _) = DomainFixtures.Build(1);
        Assert.Empty(proposal.IgnoredCommits);
    }

    [Fact]
    public void T1_AC4_IgnoredCommitsCreateNoTime()
    {
        var (_, proposal, _) = DomainFixtures.Build(2);
        // 2026-11-25 is still fully covered by work item 1205 (first commit e0f1a2b), nothing outside the week.
        Assert.All(proposal.Entries, e => Assert.InRange(e.Date, new DateOnly(2026, 11, 23), new DateOnly(2026, 11, 27)));
    }

    [Theory]
    [InlineData(1, 78, "8:15")]
    [InlineData(2, 75, "8:15")]
    public void T1_AC5_ProjectShareAndMeetingTime(int week, int share, string meeting)
    {
        var (_, _, kpis) = DomainFixtures.Build(week);
        Assert.Equal(share, kpis.ProjectSharePercent);
        Assert.Equal(meeting, TimeFormat.Hmm(kpis.MeetingMinutes));
    }

    [Theory]
    [InlineData(1, "36:40", "36:00")]
    [InlineData(2, "33:15", "32:00")]
    public void T1_AC5_WorkedVersusTarget(int week, string worked, string target)
    {
        var (_, _, kpis) = DomainFixtures.Build(week);
        Assert.Equal($"{worked} / {target}", $"{TimeFormat.Hmm(kpis.WorkedMinutes)} / {TimeFormat.Hmm(kpis.TargetMinutes)}");
    }

    [Theory]
    [InlineData(1, "1201:6:15|1202:6:15|1203:15:55")]
    [InlineData(2, "1203:6:35|1204:4:25|1205:14:00")]
    public void T1_AC6_WorkItemProjTime(int week, string expected)
    {
        var (file, proposal, _) = DomainFixtures.Build(week);
        var summary = WorkItemSummary.Build(file, proposal.Entries);
        Assert.Equal(expected.Split('|'), summary.Select(w => $"{w.Id}:{TimeFormat.Hmm(w.ProjMinutes)}"));
    }

    [Fact]
    public void T1_AC6_WorkItemsCarryTitleAndType()
    {
        var (file, proposal, _) = DomainFixtures.Build(1);
        var first = WorkItemSummary.Build(file, proposal.Entries)[0];
        Assert.Equal("Add parts search filter", first.Title);
        Assert.Equal("User Story", first.Type);
    }

    [Theory]
    [InlineData(1, "open")]
    [InlineData(2, "merged")]
    public void T1_AC6_Pr5503AppearsOncePerWeekWithState(int week, string state)
    {
        var (file, proposal, _) = DomainFixtures.Build(week);
        var prs = WorkItemSummary.Build(file, proposal.Entries).SelectMany(w => w.PullRequests).Where(p => p.Id == 5503).ToList();
        var single = Assert.Single(prs);
        Assert.Equal(state, single.State);
    }

    [Fact]
    public void T1_AC6_PullRequestsListedUnderTheirWorkItemWithState()
    {
        var (file, proposal, _) = DomainFixtures.Build(1);
        var summary = WorkItemSummary.Build(file, proposal.Entries);
        Assert.Equal("merged", summary.Single(w => w.Id == 1201).PullRequests.Single().State);
        Assert.Equal(5501, summary.Single(w => w.Id == 1201).PullRequests.Single().Id);
    }
}
