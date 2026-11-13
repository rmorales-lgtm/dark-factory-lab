using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Chronos.Tests.Infrastructure;

namespace Chronos.Tests.Pages;

public class WeekPageTests : IClassFixture<ChronosFactory>
{
    private readonly ChronosFactory _factory;

    public WeekPageTests(ChronosFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpStatusCode Status, string Html, string Text)> Get(string url)
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(url);
        var html = await response.Content.ReadAsStringAsync();
        var noTags = Regex.Replace(html, "<[^>]+>", " ");
        var text = Regex.Replace(System.Net.WebUtility.HtmlDecode(noTags), @"\s+", " ");
        return (response.StatusCode, html, text);
    }

    // ---- AC1

    [Fact]
    public async Task T1_AC1_HomeShowsWeek1AndSelectorListsBothWeeks()
    {
        var (status, html, text) = await Get("/");

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Contains("<label", html);
        Assert.Contains("value=\"2026-11-16\"", html);
        Assert.Contains("value=\"2026-11-23\"", html);
        Assert.Contains("2026-11-16 to 2026-11-20", text);
    }

    [Fact]
    public async Task T1_AC1_WeekQueryShowsWeek2()
    {
        var (status, _, text) = await Get("/?week=2026-11-23");

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Contains("2026-11-23 to 2026-11-27", text);
        Assert.DoesNotContain("2026-11-16 to 2026-11-20", text);
    }

    [Theory]
    [InlineData("/?week=2020-01-06")]
    [InlineData("/?week=")]
    [InlineData("/?week=not-a-date")]
    [InlineData("/?week=%3Cscript%3E")]
    public async Task T1_AC1_UnknownWeekShowsWeekNotFoundWith200(string url)
    {
        var (status, _, text) = await Get(url);

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Contains("Week not found", text);
    }

    [Fact]
    public async Task T1_AC1_VeryLongWeekValueDoesNotFail()
    {
        var (status, _, text) = await Get("/?week=" + new string('9', 5000));

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Contains("Week not found", text);
    }

    // ---- AC2

    [Fact]
    public async Task T1_AC2_ShowsEmployeeTeamDatesAndRolePlaceholder()
    {
        var (_, _, text) = await Get("/");

        Assert.Contains("Alex Gagnon", text);
        Assert.Contains("Team: Fictitious Squad", text);
        Assert.Contains("2026-11-16 to 2026-11-20", text);
        Assert.Contains("Role: Employee", text);
    }

    [Fact]
    public async Task T1_AC2_OneSectionPerDayWithEntriesInTimeOrder()
    {
        var (_, html, text) = await Get("/");

        foreach (var day in new[] { "2026-11-16", "2026-11-17", "2026-11-18", "2026-11-19", "2026-11-20" })
            Assert.Contains($"id=\"day-{day}\"", html);
        Assert.Equal(5, Regex.Matches(html, "class=\"day\"").Count);

        var i0800 = text.IndexOf("08:00 09:00 1:00 1201", StringComparison.Ordinal);
        var iM1 = text.IndexOf("09:00 09:15 0:15 M1", StringComparison.Ordinal);
        var iM2 = text.IndexOf("13:30 15:00 1:30 M2", StringComparison.Ordinal);
        Assert.True(i0800 >= 0 && iM1 > i0800 && iM2 > iM1);
    }

    [Fact]
    public async Task T1_AC2_EntryShowsStartEndDurationSourceAndCategory()
    {
        var (_, _, text) = await Get("/");

        Assert.Matches(@"13:30 15:00 1:30 M2 Sprint planning MGMT", text);
        Assert.Matches(@"08:00 09:00 1:00 1201 Add parts search filter PROJ", text);
        Assert.Matches(@"12:20 16:20 4:00 Sick ABSENCE", text);
        Assert.Matches(@"10:00 12:00 2:00 M8 Secure coding training 5843", text);
    }

    // ---- AC3

    [Fact]
    public async Task T1_AC3_PageWeek1()
    {
        var (_, _, text) = await Get("/");

        Assert.Contains("09:15 12:00 2:45 1201 Add parts search filter PROJ", text);
        Assert.Contains("16:00 16:30 0:30 M11 Expense reports ADMIN", text);
        Assert.Contains("14:30 15:30 1:00 M5 Architecture review MGMT", text);
        foreach (var worked in new[] { "8:00", "8:30", "4:00", "8:00", "8:10" })
            Assert.Contains($"Worked {worked}", text);
        Assert.Contains("PROJ 28:25", text);
        Assert.Contains("MGMT 5:45", text);
        Assert.Contains("5843 2:00", text);
        Assert.Contains("ADMIN 0:30", text);
        Assert.Contains("ABSENCE 4:00", text);
        Assert.Contains("36:40 / 36:00", text);
    }

    [Fact]
    public async Task T1_AC3_PageWeek2()
    {
        var (_, _, text) = await Get("/?week=2026-11-23");

        Assert.Contains("13:00 17:00 4:00 M15", text);
        Assert.Contains("08:00 12:00 4:00 Vacation ABSENCE", text);
        Assert.Contains("12:20 16:20 4:00 Vacation ABSENCE", text);
        foreach (var worked in new[] { "8:20", "8:40", "8:15", "8:00", "0:00" })
            Assert.Contains($"Worked {worked}", text);
        Assert.Contains("PROJ 25:00", text);
        Assert.Contains("MGMT 3:45", text);
        Assert.Contains("5843 4:00", text);
        Assert.Contains("ADMIN 0:30", text);
        Assert.Contains("ABSENCE 8:00", text);
        Assert.Contains("33:15 / 32:00", text);
    }

    // ---- AC4

    [Fact]
    public async Task T1_AC4_Week2HasNoM21AndListsIgnoredCommitsWithReasons()
    {
        var (_, html, text) = await Get("/?week=2026-11-23");

        Assert.DoesNotContain("M21", text);
        Assert.Contains("Ignored activity", text);
        Assert.Matches(@"f3a4b5c .*?Reason: no work item", text);
        Assert.Matches(@"b9c0d1e .*?Reason: outside the week", text);
        Assert.DoesNotContain("id=\"day-2026-11-28\"", html);
    }

    [Fact]
    public async Task T1_AC4_Week1IgnoredSectionPresent()
    {
        var (_, _, text) = await Get("/");

        Assert.Contains("Ignored activity", text);
        Assert.DoesNotContain("f3a4b5c", text);
    }

    // ---- AC5

    [Fact]
    public async Task T1_AC5_KpisWeek1()
    {
        var (_, _, text) = await Get("/");

        Assert.Contains("36:40 / 36:00", text);
        Assert.Contains("Project share 78%", text);
        Assert.Contains("Meeting time 8:15", text);
        Assert.Contains("Time per category", text);
    }

    [Fact]
    public async Task T1_AC5_KpisWeek2()
    {
        var (_, _, text) = await Get("/?week=2026-11-23");

        Assert.Contains("33:15 / 32:00", text);
        Assert.Contains("Project share 75%", text);
        Assert.Contains("Meeting time 8:15", text);
    }

    // ---- AC6

    [Fact]
    public async Task T1_AC6_WorkItemsWeek1WithOpenPullRequest()
    {
        var (_, _, text) = await Get("/");

        Assert.Contains("Work items", text);
        Assert.Contains("1201 Add parts search filter Type: User Story, PROJ time: 6:15", text);
        Assert.Contains("PR 5501 Parts search filter merged", text);
        Assert.Contains("PR 5503 Notification service refactor open", text);
        Assert.DoesNotContain("PR 5503 Notification service refactor merged", text);
        Assert.Single(Regex.Matches(text, "PR 5503"));
    }

    [Fact]
    public async Task T1_AC6_WorkItemsWeek2WithMergedPullRequest()
    {
        var (_, _, text) = await Get("/?week=2026-11-23");

        Assert.Contains("PR 5503 Notification service refactor merged", text);
        Assert.DoesNotContain("PR 5503 Notification service refactor open", text);
        Assert.Single(Regex.Matches(text, "PR 5503"));
        Assert.Contains("Type: ", text);
    }
}
