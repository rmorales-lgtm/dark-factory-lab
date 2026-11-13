using System.Net;
using System.Text.RegularExpressions;
using Chronos.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Chronos.Tests.Infrastructure;

public sealed record PageResult(HttpStatusCode Status, string Html)
{
    public string Text => Http.Text(Html);
}

public static class Http
{
    /// <summary>Strips tags, decodes HTML entities and collapses whitespace.</summary>
    public static string Text(string html)
    {
        var noTags = Regex.Replace(html, "<[^>]+>", " ");
        return Regex.Replace(WebUtility.HtmlDecode(noTags), @"\s+", " ");
    }

    public static async Task<PageResult> GetAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        return new PageResult(response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    /// <summary>GETs the page, reads the antiforgery token of its first form, then POSTs the fields to the handler.</summary>
    public static async Task<PageResult> PostFormAsync(
        HttpClient client, string pageUrl, string handler, params (string Name, string Value)[] fields)
    {
        var page = await GetAsync(client, pageUrl);
        var match = Regex.Match(page.Html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "No antiforgery token found on " + pageUrl);

        var form = fields.Select(f => new KeyValuePair<string, string>(f.Name, f.Value)).ToList();
        form.Add(new("__RequestVerificationToken", WebUtility.HtmlDecode(match.Groups[1].Value)));

        var response = await client.PostAsync("/?handler=" + handler, new FormUrlEncodedContent(form));
        return new PageResult(response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    /// <summary>Finds the data-key of the entry row whose key ends with the given suffix (e.g. "-M5").</summary>
    public static string KeyEndingWith(string html, string suffix)
    {
        var match = Regex.Match(html, "data-key=\"([^\"]*" + Regex.Escape(suffix) + ")\"");
        Assert.True(match.Success, "No entry key ending with " + suffix);
        return match.Groups[1].Value;
    }

    public static T InDb<T>(ChronosFactory factory, Func<ChronosDbContext, T> read)
    {
        using var scope = factory.Services.CreateScope();
        return read(scope.ServiceProvider.GetRequiredService<ChronosDbContext>());
    }

    public static int SavedEntryCount(ChronosFactory factory, string? weekStart = null)
        => InDb(factory, db => db.SavedEntries.AsNoTracking().Count(e => weekStart == null || e.WeekStart == weekStart));
}
