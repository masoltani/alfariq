namespace AlFariq.Domain.Tests;

public class StatsPeriodTests
{
    private static readonly DateOnly Today = new(2026, 11, 28);

    [Fact]
    public void All_matches_every_day()
    {
        var period = StatsPeriod.Of(PeriodKind.All, Today, from: Today, to: Today);

        Assert.Equal(new StatsPeriod(), period);
        Assert.True(period.Contains(DateOnly.MinValue));
        Assert.True(period.Contains(DateOnly.MaxValue));
    }

    [Fact]
    public void The_month_goes_from_its_first_to_its_last_day()
    {
        var period = StatsPeriod.Of(PeriodKind.Month, new DateOnly(2028, 2, 10));

        Assert.Equal(new StatsPeriod(new DateOnly(2028, 2, 1), new DateOnly(2028, 2, 29)), period);
    }

    [Theory]
    [InlineData("2026-07-01", 2026)]
    [InlineData("2026-11-28", 2026)]
    [InlineData("2027-06-30", 2026)]
    [InlineData("2027-07-01", 2027)]
    public void The_season_runs_from_july_to_june(string day, int firstYear)
    {
        var period = StatsPeriod.Of(PeriodKind.Season, DateOnly.ParseExact(day, "yyyy-MM-dd"));

        Assert.Equal(new StatsPeriod(new DateOnly(firstYear, 7, 1), new DateOnly(firstYear + 1, 6, 30)), period);
    }

    [Fact]
    public void Chosen_dates_may_be_open_ended()
    {
        var since = StatsPeriod.Of(PeriodKind.Custom, Today, from: Today);

        Assert.True(since.Contains(Today.AddYears(1)));
        Assert.False(since.Contains(Today.AddDays(-1)));
        Assert.False(since.IsReversed);
    }

    [Fact]
    public void Reversed_dates_match_nothing()
    {
        var period = StatsPeriod.Of(PeriodKind.Custom, Today, from: Today, to: Today.AddDays(-1));

        Assert.True(period.IsReversed);
        Assert.False(period.Contains(Today));
        Assert.False(period.Contains(Today.AddDays(-1)));
    }
}
