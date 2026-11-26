using AlFariq.Domain;

namespace AlFariq.Domain.Tests;

public class MatchScheduleTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 25, 10, 0, 0, TimeSpan.Zero);

    private static Match At(double hoursFromNow) => new() { StartsAt = Now.AddHours(hoursFromNow), PlayersPerSide = 5 };

    [Fact]
    public void Upcoming_matches_include_the_one_in_progress_and_come_closest_first()
    {
        var later = At(48);
        var inProgress = At(-0.5);
        var soon = At(2);
        var over = At(-1); // une heure de jeu : tout juste terminé

        var upcoming = MatchSchedule.Upcoming([later, over, soon, inProgress], Now);

        Assert.Equal(new[] { inProgress, soon, later }, upcoming);
    }

    [Fact]
    public void Past_matches_come_most_recent_first()
    {
        var old = At(-72);
        var recent = At(-2);
        var future = At(2);

        var past = MatchSchedule.Past([old, future, recent], Now);

        Assert.Equal(new[] { recent, old }, past);
    }

    [Fact]
    public void A_date_and_time_keep_their_value_in_their_time_zone()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");

        var instant = MatchSchedule.At(new DateOnly(2027, 7, 1), new TimeOnly(20, 30), zone);

        Assert.Equal(TimeSpan.FromHours(2), instant.Offset);
        Assert.Equal(new DateTime(2027, 7, 1, 20, 30, 0), MatchSchedule.ToLocal(instant.ToUniversalTime(), zone));
    }
}
