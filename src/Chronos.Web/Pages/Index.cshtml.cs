using Chronos.Web.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Chronos.Web.Pages;

public class IndexModel : PageModel
{
    public const string DefaultWeek = "2026-11-16";

    private readonly IWeekDataSource _source;

    public IndexModel(IWeekDataSource source)
    {
        _source = source;
    }

    public IReadOnlyList<string> AvailableWeeks { get; private set; } = [];
    public string SelectedWeek { get; private set; } = DefaultWeek;
    public bool WeekNotFound { get; private set; }

    public WeekFile? Week { get; private set; }
    public WeekProposal Proposal { get; private set; } = new([], []);
    public WeekKpis? Kpis { get; private set; }
    public IReadOnlyList<WorkItemSummary> WorkItems { get; private set; } = [];

    public IEnumerable<IGrouping<DateOnly, TimeEntry>> Days
        => Proposal.Entries.GroupBy(e => e.Date).OrderBy(g => g.Key);

    public void OnGet([FromQuery] string? week)
    {
        AvailableWeeks = _source.WeekStarts;
        var requested = Request.Query.ContainsKey("week") ? (week ?? "") : DefaultWeek;
        var data = _source.Get(requested);
        if (data is null)
        {
            WeekNotFound = true;
            return;
        }

        SelectedWeek = requested;
        Week = data;
        Proposal = ProposalBuilder.Build(data);
        Kpis = KpiCalculator.Calculate(data, Proposal.Entries);
        WorkItems = WorkItemSummary.Build(data, Proposal.Entries);
    }

    public static string Hmm(int minutes) => TimeFormat.Hmm(minutes);
}
