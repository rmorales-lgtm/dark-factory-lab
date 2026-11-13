using Chronos.Web.Domain;

namespace Chronos.Tests.Domain;

public class EntryInputValidatorTests
{
    private static readonly DateOnly WeekStart = new(2026, 11, 16);
    private static readonly DateOnly WeekEnd = new(2026, 11, 20);

    private static IReadOnlyList<InputError> Check(DateOnly date, string start, string end, string? category)
        => EntryInputValidator.Validate(WeekStart, WeekEnd, date, TimeOnly.Parse(start), TimeOnly.Parse(end), category);

    [Fact]
    public void T2_AC2_ValidInputHasNoError()
        => Assert.Empty(Check(new DateOnly(2026, 11, 17), "09:00", "10:00", "PROJ"));

    [Theory]
    [InlineData("PROJ")]
    [InlineData("MGMT")]
    [InlineData("ADMIN")]
    [InlineData("5843")]
    [InlineData("ABSENCE")]
    [InlineData("")]
    [InlineData(null)]
    public void T2_AC2_ValidOrEmptyCategoryAccepted(string? category)
        => Assert.Empty(Check(new DateOnly(2026, 11, 17), "09:00", "10:00", category));

    [Theory]
    [InlineData("10:00", "10:00")]
    [InlineData("10:00", "09:00")]
    public void T2_AC2_EndAtOrBeforeStartRefused(string start, string end)
    {
        var error = Assert.Single(Check(new DateOnly(2026, 11, 17), start, end, "PROJ"));
        Assert.Equal("End", error.Field);
        Assert.Equal("End must be after start", error.Message);
    }

    [Theory]
    [InlineData(2026, 11, 15)]
    [InlineData(2026, 11, 21)]
    public void T2_AC2_DateOutsideWeekRefused(int y, int m, int d)
    {
        var error = Assert.Single(Check(new DateOnly(y, m, d), "09:00", "10:00", "PROJ"));
        Assert.Equal("Date", error.Field);
        Assert.Equal("Date must be within the week", error.Message);
    }

    [Theory]
    [InlineData("proj")]
    [InlineData("XYZ")]
    public void T2_AC2_UnknownCategoryRefused(string category)
    {
        var error = Assert.Single(Check(new DateOnly(2026, 11, 17), "09:00", "10:00", category));
        Assert.Equal("Category", error.Field);
        Assert.Equal("Unknown category", error.Message);
    }

    [Fact]
    public void T2_AC2_SeveralErrorsAreAllReported()
        => Assert.Equal(3, Check(new DateOnly(2026, 12, 1), "10:00", "09:00", "XYZ").Count);
}
