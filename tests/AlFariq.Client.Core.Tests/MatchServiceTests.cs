using AlFariq.Application;
using AlFariq.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace AlFariq.Client.Core.Tests;

public sealed class MatchServiceTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 12, 2);
    private static readonly TimeOnly Evening = new(20, 0);

    private readonly FakeClock _clock = new();
    private readonly ServiceProvider _app;
    private readonly MatchService _matches;

    public MatchServiceTests()
    {
        _app = MatchTestHost.Create(_clock);
        _matches = _app.GetRequiredService<MatchService>();
    }

    public void Dispose() => _app.Dispose();

    private PlayerService Players => _app.GetRequiredService<PlayerService>();

    private IRepository<MatchParticipant> Participants =>
        _app.GetRequiredService<IRepository<MatchParticipant>>();

    [Fact]
    public async Task Date_time_and_format_are_enough_and_the_match_is_planned()
    {
        var created = await _matches.CreateAsync(new MatchInput(Day, Evening, 5, Location: "  ", Notes: ""));

        var stored = Assert.Single(await _matches.ListAsync());
        Assert.Equal(created.Id, stored.Id);
        Assert.Equal(new DateTimeOffset(2026, 12, 2, 20, 0, 0, TimeSpan.Zero), stored.StartsAt);
        Assert.Equal(5, stored.PlayersPerSide);
        Assert.Equal(SportCatalog.FootballCode, stored.SportCode);
        Assert.Equal(60, stored.DurationMinutes);
        Assert.Null(stored.Location);
        Assert.Null(stored.Notes);
        Assert.Equal(MatchStatus.Planned, stored.Status);
    }

    [Theory]
    [InlineData(1999, "football", 5, 60)]
    [InlineData(2101, "football", 5, 60)]
    [InlineData(2026, "inconnu", 5, 60)]
    [InlineData(2026, "football", 0, 60)]
    [InlineData(2026, "football", 31, 60)]
    [InlineData(2026, "football", 5, 9)]
    [InlineData(2026, "football", 5, 301)]
    public async Task Invalid_input_is_rejected_and_nothing_is_written(int year, string sport, int format, int duration)
    {
        var input = new MatchInput(new DateOnly(year, 12, 2), Evening, format, sport, duration);

        await Assert.ThrowsAsync<ArgumentException>(() => _matches.CreateAsync(input));

        Assert.Empty(await _matches.ListAsync());
    }

    [Fact]
    public async Task Too_long_texts_are_rejected()
    {
        var location = new string('a', Match.MaxLocationLength + 1);
        var notes = new string('a', Match.MaxNotesLength + 1);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _matches.CreateAsync(new MatchInput(Day, Evening, 5, Location: location)));
        await Assert.ThrowsAsync<ArgumentException>(
            () => _matches.CreateAsync(new MatchInput(Day, Evening, 5, Notes: notes)));

        Assert.Empty(await _matches.ListAsync());
    }

    [Fact]
    public async Task A_match_can_be_edited_including_its_format()
    {
        var created = await _matches.CreateAsync(new MatchInput(Day, Evening, 5, Location: "Stade"));
        var input = (await _matches.GetInputAsync(created.Id))!;

        await _matches.UpdateAsync(created.Id, input with { PlayersPerSide = 8, Location = null, Notes = "Chasubles" });

        var stored = Assert.Single(await _matches.ListAsync());
        Assert.Equal(created.Id, stored.Id);
        Assert.Equal(created.StartsAt, stored.StartsAt);
        Assert.Equal(8, stored.PlayersPerSide);
        Assert.Null(stored.Location);
        Assert.Equal("Chasubles", stored.Notes);
        Assert.Equal(MatchStatus.Planned, stored.Status);
    }

    [Fact]
    public async Task A_deleted_match_is_hidden_but_kept()
    {
        var created = await _matches.CreateAsync(new MatchInput(Day, Evening, 5));

        Assert.True(await _matches.DeleteAsync(created.Id));

        Assert.Empty(await _matches.ListAsync());
        Assert.Null(await _matches.FindAsync(created.Id));
        var repository = _app.GetRequiredService<IRepository<Match>>();
        Assert.True((await repository.GetAsync(created.Id, includeDeleted: true))!.IsDeleted);
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _matches.UpdateAsync(created.Id, new MatchInput(Day, Evening, 5)));
    }

    [Fact]
    public async Task Duplicating_prefills_the_same_match_seven_days_later_without_saving_anything()
    {
        var source = await _matches.CreateAsync(
            new MatchInput(Day, Evening, 7, "football", 90, "Stade", "Apporter le ballon"));

        var copy = await _matches.GetDuplicateInputAsync(source.Id);

        Assert.Equal(new MatchInput(Day.AddDays(7), Evening, 7, "football", 90, "Stade"), copy);
        Assert.Single(await _matches.ListAsync());
    }

    [Fact]
    public async Task Duplicating_keeps_the_local_time_across_a_daylight_saving_change()
    {
        _clock.Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");
        var source = await _matches.CreateAsync(new MatchInput(new DateOnly(2027, 3, 24), Evening, 5));

        var copy = await _matches.CreateAsync((await _matches.GetDuplicateInputAsync(source.Id))!, source.Id);

        Assert.Equal(new DateTimeOffset(2027, 3, 24, 20, 0, 0, TimeSpan.FromHours(1)), source.StartsAt);
        Assert.Equal(new DateTimeOffset(2027, 3, 31, 20, 0, 0, TimeSpan.FromHours(2)), copy.StartsAt);
    }

    [Fact]
    public async Task Saving_a_duplicate_copies_the_players_except_archived_ones()
    {
        var alice = await Players.AddAsync(new PlayerInput("Alice"));
        var sami = await Players.AddAsync(new PlayerInput("Sami"));
        var source = await _matches.CreateAsync(new MatchInput(Day, Evening, 5));
        await Participants.AddAsync(new MatchParticipant { MatchId = source.Id, PlayerId = alice.Id });
        await Participants.AddAsync(new MatchParticipant { MatchId = source.Id, PlayerId = sami.Id });
        await Players.SetArchivedAsync(sami.Id, archived: true);

        var copy = await _matches.CreateAsync((await _matches.GetDuplicateInputAsync(source.Id))!, source.Id);

        var all = await Participants.ListAsync();
        Assert.Equal(new[] { alice.Id }, all.Where(p => p.MatchId == copy.Id).Select(p => p.PlayerId));
        Assert.Equal(2, all.Count(p => p.MatchId == source.Id));
    }

    [Fact]
    public async Task A_missing_match_cannot_be_duplicated()
    {
        var source = await _matches.CreateAsync(new MatchInput(Day, Evening, 5));
        await _matches.DeleteAsync(source.Id);

        Assert.Null(await _matches.GetDuplicateInputAsync(source.Id));
    }
}
