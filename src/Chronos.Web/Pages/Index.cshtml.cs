using System.Globalization;
using Chronos.Web.Domain;
using Chronos.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Chronos.Web.Pages;

public class IndexModel : PageModel
{
    public const string DefaultWeek = "2026-11-16";
    public const string AddForm = "add";

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private readonly IWeekDataSource _source;
    private readonly WeekService _weeks;

    private readonly Dictionary<string, string> _errors = new();
    private readonly Dictionary<string, string> _values = new();

    public IndexModel(IWeekDataSource source, WeekService weeks)
    {
        _source = source;
        _weeks = weeks;
    }

    public IReadOnlyList<string> AvailableWeeks { get; private set; } = [];
    public string SelectedWeek { get; private set; } = DefaultWeek;
    public bool WeekNotFound { get; private set; }

    public WeekFile? Week { get; private set; }
    public WeekProposal Proposal { get; private set; } = new([], []);
    public IReadOnlyList<TimeEntry> Entries { get; private set; } = [];
    public WeekKpis? Kpis { get; private set; }
    public IReadOnlyList<WorkItemSummary> WorkItems { get; private set; } = [];

    /// <summary>True when the controls that change entries are shown (status Draft or Rejected).</summary>
    public bool CanEdit { get; private set; }

    public IEnumerable<IGrouping<DateOnly, TimeEntry>> Days
        => Entries.GroupBy(e => e.Date).OrderBy(g => g.Key);

    public static string EditForm(string key) => "edit-" + key;

    public string? Error(string form, string field) => _errors.GetValueOrDefault(form + "|" + field);
    public bool HasErrors(string form) => _errors.Keys.Any(k => k.StartsWith(form + "|", StringComparison.Ordinal));
    public string Value(string form, string field, string fallback) => _values.GetValueOrDefault(form + "|" + field, fallback);

    public void OnGet([FromQuery] string? week)
    {
        var requested = Request.Query.ContainsKey("week") ? (week ?? "") : DefaultWeek;
        Load(requested);
    }

    public IActionResult OnPostEdit(string? week, string? key, string? start, string? end, string? category)
    {
        if (!Load(week)) return Page();
        if (_weeks.CheckEditable(SelectedWeek) is { } block) return Blocked(block);

        category = category?.Trim();
        var entry = Entries.FirstOrDefault(e => e.Key == key);
        if (entry is null) return NotFound();

        var form = EditForm(entry.Key);
        Remember(form, ("Start", start), ("End", end), ("Category", category));
        var parsed = ParseTimes(form, start, end, out var s, out var e);
        if (parsed)
            parsed = Validate(form, entry.Date, s, e, category);
        if (!parsed) return Page();

        _weeks.Edit(Week!, entry.Key, s, e, category);
        return RedirectToWeek();
    }

    public IActionResult OnPostDelete(string? week, string? key)
    {
        if (!Load(week)) return Page();
        if (_weeks.CheckEditable(SelectedWeek) is { } block) return Blocked(block);

        if (!_weeks.Delete(Week!, key ?? "")) return NotFound();
        return RedirectToWeek();
    }

    public IActionResult OnPostAdd(string? week, string? date, string? start, string? end, string? category)
    {
        if (!Load(week)) return Page();
        if (_weeks.CheckEditable(SelectedWeek) is { } block) return Blocked(block);

        category = category?.Trim();
        Remember(AddForm, ("Date", date), ("Start", start), ("End", end), ("Category", category));
        var ok = ParseTimes(AddForm, start, end, out var s, out var e);
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", Inv, DateTimeStyles.None, out var d))
        {
            _errors[AddForm + "|Date"] = EntryInputValidator.DateInWeek;
            ok = false;
        }
        if (ok)
            ok = Validate(AddForm, d, s, e, category);
        if (!ok) return Page();

        _weeks.Add(Week!, d, s, e, category);
        return RedirectToWeek();
    }

    public IActionResult OnPostReset(string? week)
    {
        if (!Load(week)) return Page();
        if (_weeks.CheckEditable(SelectedWeek) is { } block) return Blocked(block);

        _weeks.Reset(SelectedWeek);
        return RedirectToWeek();
    }

    public static string Hmm(int minutes) => TimeFormat.Hmm(minutes);

    private bool Load(string? requested)
    {
        AvailableWeeks = _source.WeekStarts;
        var data = _source.Get(requested);
        if (data is null)
        {
            WeekNotFound = true;
            return false;
        }

        SelectedWeek = requested!;
        Week = data;
        Proposal = ProposalBuilder.Build(data);
        Entries = _weeks.GetEntries(data);
        Kpis = KpiCalculator.Calculate(data, Entries);
        WorkItems = WorkItemSummary.Build(data, Entries);
        CanEdit = _weeks.CheckEditable(SelectedWeek) is null;
        return true;
    }

    private bool ParseTimes(string form, string? start, string? end, out TimeOnly s, out TimeOnly e)
    {
        var okStart = TimeOnly.TryParseExact(start, "HH:mm", Inv, DateTimeStyles.None, out s);
        var okEnd = TimeOnly.TryParseExact(end, "HH:mm", Inv, DateTimeStyles.None, out e);
        if (!okStart) _errors[form + "|Start"] = "Start must be a time (HH:mm)";
        if (!okEnd) _errors[form + "|End"] = "End must be a time (HH:mm)";
        return okStart && okEnd;
    }

    private bool Validate(string form, DateOnly date, TimeOnly start, TimeOnly end, string? category)
    {
        var errors = EntryInputValidator.Validate(Week!.StartDate, Week.EndDate, date, start, end, category);
        foreach (var err in errors)
            _errors[form + "|" + err.Field] = err.Message;
        return errors.Count == 0;
    }

    private void Remember(string form, params (string Field, string? Value)[] fields)
    {
        foreach (var (field, value) in fields)
            _values[form + "|" + field] = value ?? "";
    }

    private IActionResult Blocked(EditBlock block)
        => new ContentResult { StatusCode = block.StatusCode, Content = block.Message, ContentType = "text/plain" };

    private IActionResult RedirectToWeek() => RedirectToPage("/Index", new { week = SelectedWeek });
}
