using System.Text.Json;

namespace AlFariq.Domain.Tests;

public class MatchResultTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 25, 10, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(2, 1, TeamSide.A)]
    [InlineData(0, 3, TeamSide.B)]
    [InlineData(1, 1, null)]
    public void The_result_is_a_win_or_a_draw(int a, int b, TeamSide? winner)
    {
        Assert.Equal(winner, new MatchScore(a, b).Winner);
    }

    [Theory]
    [InlineData(0, false)] // aucun buteur saisi : rien à comparer
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(4, true)]
    public void A_gap_between_scorers_and_score_is_reported(int playerGoals, bool expected)
    {
        Assert.Equal(expected, new MatchScore(3, 0).DiffersFrom(TeamSide.A, playerGoals));
    }

    [Fact]
    public void A_scorer_in_a_team_without_goal_is_a_gap()
    {
        Assert.True(new MatchScore(3, 0).DiffersFrom(TeamSide.B, 1));
    }

    [Fact]
    public void A_finished_match_is_past_even_before_its_planned_end()
    {
        var finished = new Match { StartsAt = Now.AddHours(2), Status = MatchStatus.Finished };
        var cancelled = new Match { StartsAt = Now.AddHours(2), Status = MatchStatus.Cancelled };

        Assert.Equal(new[] { cancelled }, MatchSchedule.Upcoming([finished, cancelled], Now));
        Assert.Equal(new[] { finished }, MatchSchedule.Past([finished, cancelled], Now));
    }

    [Theory]
    [InlineData(MatchStatus.Planned)]
    [InlineData(MatchStatus.Finished)]
    [InlineData(MatchStatus.Cancelled)]
    public void A_status_is_stored_as_text(MatchStatus status)
    {
        var json = JsonSerializer.Serialize(new Match { Status = status });

        Assert.Contains($"\"Status\":\"{status}\"", json);
    }

    [Theory]
    [InlineData("football", true)]
    [InlineData("futsal", true)]
    [InlineData("handball", true)]
    [InlineData("basketball", false)]
    [InlineData("volleyball", false)]
    [InlineData(null, false)]
    public void Only_some_sports_count_goals(string? sport, bool expected)
    {
        Assert.Equal(expected, SportCatalog.CountsGoals(sport));
    }
}
