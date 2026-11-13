using System.Net;
using Chronos.Tests.Infrastructure;

namespace Chronos.Tests.Pages;

public class EditPageTests
{
    private const string Week1 = "/?week=2026-11-16";
    private const string Week2 = "/?week=2026-11-23";

    [Fact]
    public async Task T2_AC1_EditPersistsAfterReload()
    {
        using var factory = new ChronosFactory();
        var client = factory.CreateClient();
        var key = Http.KeyEndingWith((await Http.GetAsync(client, Week1)).Html, "-M5");

        var result = await Http.PostFormAsync(client, Week1, "Edit",
            ("week", "2026-11-16"), ("key", key), ("start", "15:00"), ("end", "15:30"), ("category", "ADMIN"));

        Assert.Equal(HttpStatusCode.OK, result.Status);
        Assert.Contains("15:00 15:30 0:30 M5 Architecture review ADMIN", result.Text);

        var reloaded = await Http.GetAsync(factory.CreateClient(), Week1);
        Assert.Contains("15:00 15:30 0:30 M5 Architecture review ADMIN", reloaded.Text);
        Assert.Contains(key, reloaded.Html);
        // KPIs are recalculated from the saved entries: 36:40 - 0:30 = 36:10
        Assert.Contains("36:10 / 36:00", reloaded.Text);
    }

    [Fact]
    public async Task T2_AC1_DeleteAndAdd()
    {
        using var factory = new ChronosFactory();
        var client = factory.CreateClient();
        var key = Http.KeyEndingWith((await Http.GetAsync(client, Week1)).Html, "-M11");

        var deleted = await Http.PostFormAsync(client, Week1, "Delete", ("week", "2026-11-16"), ("key", key));
        Assert.DoesNotContain("M11 Expense reports", deleted.Text);
        Assert.Contains("ADMIN 0:00", deleted.Text);

        var added = await Http.PostFormAsync(client, Week1, "Add",
            ("week", "2026-11-16"), ("date", "2026-11-18"), ("start", "16:20"), ("end", "17:20"), ("category", "MGMT"));
        Assert.Contains("16:20 17:20 1:00 Manual entry MGMT", added.Text);

        var reloaded = await Http.GetAsync(factory.CreateClient(), Week1);
        Assert.DoesNotContain("M11 Expense reports", reloaded.Text);
        Assert.Contains("16:20 17:20 1:00 Manual entry MGMT", reloaded.Text);
        Assert.Contains("manual-", reloaded.Html);
    }

    [Fact]
    public async Task T2_AC1_DeletingEveryEntryStaysSaved()
    {
        using var factory = new ChronosFactory();
        var client = factory.CreateClient();
        var html = (await Http.GetAsync(client, Week1)).Html;
        var keys = System.Text.RegularExpressions.Regex.Matches(html, "data-key=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToList();

        foreach (var key in keys)
            await Http.PostFormAsync(client, Week1, "Delete", ("week", "2026-11-16"), ("key", key));

        var reloaded = await Http.GetAsync(factory.CreateClient(), Week1);
        Assert.DoesNotContain("data-key=", reloaded.Html);
        Assert.Equal(0, Http.SavedEntryCount(factory, "2026-11-16"));
    }

    [Fact]
    public async Task T2_AC1_OtherWeekUntouched()
    {
        using var factory = new ChronosFactory();
        var client = factory.CreateClient();
        var week2Before = (await Http.GetAsync(client, Week2)).Text;
        var key = Http.KeyEndingWith((await Http.GetAsync(client, Week1)).Html, "-M5");

        await Http.PostFormAsync(client, Week1, "Edit",
            ("week", "2026-11-16"), ("key", key), ("start", "15:00"), ("end", "15:30"), ("category", "MGMT"));

        Assert.Equal(0, Http.SavedEntryCount(factory, "2026-11-23"));
        Assert.True(Http.SavedEntryCount(factory, "2026-11-16") > 0);
        var week2After = (await Http.GetAsync(client, Week2)).Text;
        Assert.Equal(week2Before, week2After);
        Assert.Contains("33:15 / 32:00", week2After);
    }

    [Fact]
    public async Task T2_AC1_ResetToProposal()
    {
        using var factory = new ChronosFactory();
        var client = factory.CreateClient();

        var key = Http.KeyEndingWith((await Http.GetAsync(client, Week1)).Html, "-M5");
        await Http.PostFormAsync(client, Week1, "Edit",
            ("week", "2026-11-16"), ("key", key), ("start", "15:00"), ("end", "15:30"), ("category", "MGMT"));
        await Http.PostFormAsync(client, Week1, "Add",
            ("week", "2026-11-16"), ("date", "2026-11-18"), ("start", "16:20"), ("end", "17:20"), ("category", "MGMT"));
        Assert.True(Http.SavedEntryCount(factory) > 0);

        var reset = await Http.PostFormAsync(client, Week1, "Reset", ("week", "2026-11-16"));

        Assert.Contains("Reset to proposal", reset.Text);
        Assert.Contains("14:30 15:30 1:00 M5 Architecture review MGMT", reset.Text);
        Assert.DoesNotContain("Manual entry", reset.Text);
        Assert.Contains("36:40 / 36:00", reset.Text);
        Assert.Equal(0, Http.SavedEntryCount(factory));
    }

    [Fact]
    public async Task T2_AC2_EndBeforeStartRefused()
    {
        using var factory = new ChronosFactory();
        var client = factory.CreateClient();
        var key = Http.KeyEndingWith((await Http.GetAsync(client, Week1)).Html, "-M5");

        var result = await Http.PostFormAsync(client, Week1, "Edit",
            ("week", "2026-11-16"), ("key", key), ("start", "15:00"), ("end", "14:00"), ("category", "MGMT"));

        Assert.Contains("End must be after start", result.Text);
        Assert.Contains("14:30 15:30 1:00 M5 Architecture review MGMT", result.Text);
        Assert.Equal(0, Http.SavedEntryCount(factory));

        var added = await Http.PostFormAsync(client, Week1, "Add",
            ("week", "2026-11-16"), ("date", "2026-11-18"), ("start", "16:00"), ("end", "16:00"), ("category", "MGMT"));
        Assert.Contains("End must be after start", added.Text);
        Assert.Equal(0, Http.SavedEntryCount(factory));
    }

    [Fact]
    public async Task T2_AC2_DateOutsideWeekRefused()
    {
        using var factory = new ChronosFactory();
        var client = factory.CreateClient();

        var result = await Http.PostFormAsync(client, Week1, "Add",
            ("week", "2026-11-16"), ("date", "2026-11-23"), ("start", "09:00"), ("end", "10:00"), ("category", "MGMT"));

        Assert.Contains("Date must be within the week", result.Text);
        Assert.Equal(0, Http.SavedEntryCount(factory));
        Assert.DoesNotContain("Manual entry", (await Http.GetAsync(client, Week1)).Text);
    }

    [Fact]
    public async Task T2_AC2_UnknownCategoryRefused()
    {
        using var factory = new ChronosFactory();
        var client = factory.CreateClient();
        var key = Http.KeyEndingWith((await Http.GetAsync(client, Week1)).Html, "-M5");

        var edited = await Http.PostFormAsync(client, Week1, "Edit",
            ("week", "2026-11-16"), ("key", key), ("start", "14:30"), ("end", "15:30"), ("category", "NOPE"));
        Assert.Contains("Unknown category", edited.Text);
        Assert.Equal(0, Http.SavedEntryCount(factory));

        var added = await Http.PostFormAsync(client, Week1, "Add",
            ("week", "2026-11-16"), ("date", "2026-11-18"), ("start", "16:20"), ("end", "17:00"), ("category", "NOPE"));
        Assert.Contains("Unknown category", added.Text);
        Assert.Equal(0, Http.SavedEntryCount(factory));
    }

    [Fact]
    public async Task T2_AC2_PostWithoutAntiforgeryTokenIsRefused()
    {
        using var factory = new ChronosFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/?handler=Reset",
            new FormUrlEncodedContent([new KeyValuePair<string, string>("week", "2026-11-16")]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
