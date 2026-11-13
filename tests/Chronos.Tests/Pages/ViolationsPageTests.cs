using System.Text.RegularExpressions;
using Chronos.Tests.Infrastructure;

namespace Chronos.Tests.Pages;

public class ViolationsPageTests
{
    private const string Week1 = "/?week=2026-11-16";
    private const string Week2 = "/?week=2026-11-23";

    private static string[] Expected(string week) => week == Week1
        ?
        [
            "Overlap: M4 and M5 on 2026-11-17",
            "Total 36:40 does not match target 36:00",
        ]
        :
        [
            "Overlap: M17 and M18 on 2026-11-25",
            "Break: no 20-minute break on 2026-11-23",
            "Total 33:15 does not match target 32:00",
        ];

    private static string ViolationsPanel(string html)
    {
        var m = Regex.Match(html, "<section id=\"violations\".*?</section>", RegexOptions.Singleline);
        Assert.True(m.Success, "Violations panel not found");
        return Http.Text(m.Value);
    }

    [Theory]
    [InlineData(Week1)]
    [InlineData(Week2)]
    public async Task T2_AC3_ViolationsPanel(string week)
    {
        using var factory = new ChronosFactory();
        var page = await Http.GetAsync(factory.CreateClient(), week);

        var panel = ViolationsPanel(page.Html);
        Assert.Contains("Violations", panel);
        Assert.DoesNotContain("Week is valid", panel);
        foreach (var message in Expected(week))
            Assert.Contains(message, panel);
        Assert.Equal(Expected(week).Length, Regex.Matches(page.Html, "class=\"viol\"").Count);
    }

    [Fact]
    public async Task T2_AC3_TrainingM8ToMgmtPage()
    {
        using var factory = new ChronosFactory();
        var client = factory.CreateClient();
        var key = Http.KeyEndingWith((await Http.GetAsync(client, Week1)).Html, "-M8");

        var result = await Http.PostFormAsync(client, Week1, "Edit",
            ("week", "2026-11-16"), ("key", key), ("start", ExistingTime(key, true, (await Http.GetAsync(client, Week1)).Html)),
            ("end", ExistingTime(key, false, (await Http.GetAsync(client, Week1)).Html)), ("category", "MGMT"));

        Assert.Contains("Training must use code 5843: M8 on 2026-11-19", ViolationsPanel(result.Html));
    }

    [Theory]
    [InlineData(Week1, 2)]
    [InlineData(Week2, 3)]
    public async Task T2_AC4_ViolationCountKpi(string week, int count)
    {
        using var factory = new ChronosFactory();
        var page = await Http.GetAsync(factory.CreateClient(), week);

        Assert.Contains($"Violations: {count}", page.Text);
    }

    [Fact]
    public async Task T2_AC4_KpisRecalculatedAfterEdit()
    {
        using var factory = new ChronosFactory();
        var client = factory.CreateClient();
        var key = Http.KeyEndingWith((await Http.GetAsync(client, Week1)).Html, "-M5");

        var result = await Http.PostFormAsync(client, Week1, "Edit",
            ("week", "2026-11-16"), ("key", key), ("start", "15:00"), ("end", "15:30"), ("category", "ADMIN"));

        // 36:40 - 0:30 = 36:10; M5 moved from MGMT to ADMIN; overlap with M4 is gone; the total still differs.
        Assert.Contains("36:10 / 36:00", result.Text);
        Assert.Contains("Violations: 1", result.Text);
        Assert.DoesNotContain("Overlap: M4 and M5", result.Text);
        Assert.Contains("Total 36:10 does not match target 36:00", ViolationsPanel(result.Html));
    }

    [Fact]
    public async Task T2_AC5_ValidScenarioPage()
    {
        using var factory = new ChronosFactory();
        var client = factory.CreateClient();
        var html = (await Http.GetAsync(client, Week1)).Html;
        var m5 = Http.KeyEndingWith(html, "-M5");
        var longKey = Regex.Match(html, "data-key=\"(2026-11-20T12:20-[^\"]*)\"");
        Assert.True(longKey.Success, "Entry 2026-11-20 12:20 not found");
        var row = Regex.Match(html, "data-key=\"" + Regex.Escape(longKey.Groups[1].Value) + "\".*?</tr>", RegexOptions.Singleline).Value;
        var category = Regex.Match(row, "name=\"category\"[^>]*value=\"([^\"]*)\"").Groups[1].Value;

        await Http.PostFormAsync(client, Week1, "Edit",
            ("week", "2026-11-16"), ("key", m5), ("start", "15:00"), ("end", "15:30"), ("category", "MGMT"));
        var result = await Http.PostFormAsync(client, Week1, "Edit",
            ("week", "2026-11-16"), ("key", longKey.Groups[1].Value), ("start", "12:30"), ("end", "15:00"), ("category", category));

        Assert.Contains("36:00 / 36:00", result.Text);
        Assert.Contains("Violations: 0", result.Text);
        Assert.Contains("Week is valid", ViolationsPanel(result.Html));
    }

    [Theory]
    [InlineData(Week1)]
    [InlineData(Week2)]
    public async Task T2_AC6_PageMessagesBothWeeks(string week)
    {
        using var factory = new ChronosFactory();
        var page = await Http.GetAsync(factory.CreateClient(), week);

        foreach (var message in Expected(week))
            Assert.Contains(message, page.Text);
    }

    private static string ExistingTime(string key, bool start, string html)
    {
        var row = Regex.Match(html, "data-key=\"" + Regex.Escape(key) + "\".*?</tr>", RegexOptions.Singleline).Value;
        var name = start ? "start" : "end";
        return Regex.Match(row, "name=\"" + name + "\"[^>]*value=\"([^\"]*)\"").Groups[1].Value;
    }
}
