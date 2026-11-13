namespace Chronos.Web.Domain;

public sealed record InputError(string Field, string Message);

/// <summary>Validates the input of an entry form (pure, no web dependency).</summary>
public static class EntryInputValidator
{
    public static readonly string[] ValidCategories = ["PROJ", "MGMT", "ADMIN", "5843", "ABSENCE"];

    public const string EndAfterStart = "End must be after start";
    public const string DateInWeek = "Date must be within the week";
    public const string UnknownCategory = "Unknown category";

    /// <summary>An empty category is allowed (the entry has no category).</summary>
    public static IReadOnlyList<InputError> Validate(
        DateOnly weekStart, DateOnly weekEnd, DateOnly date, TimeOnly start, TimeOnly end, string? category)
    {
        var errors = new List<InputError>();
        if (end <= start)
            errors.Add(new InputError("End", EndAfterStart));
        if (date < weekStart || date > weekEnd)
            errors.Add(new InputError("Date", DateInWeek));
        if (!string.IsNullOrEmpty(category) && !ValidCategories.Contains(category, StringComparer.Ordinal))
            errors.Add(new InputError("Category", UnknownCategory));
        return errors;
    }
}
