using AlFariq.Application;
using AlFariq.Client.Core.Storage;
using AlFariq.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace AlFariq.Client.Core.Tests;

public sealed class PlayerServiceTests : IDisposable
{
    private readonly ServiceProvider _app;
    private readonly PlayerService _players;

    public PlayerServiceTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDocumentStore>(new InMemoryDocumentStore());
        services.AddSingleton<TimeProvider>(new FakeClock());
        services.AddAlFariqApplication();
        services.AddLocalRepository<Profile>(LocalSchema.ProfilesStore);
        services.AddLocalRepository<Player>(LocalSchema.PlayersStore);
        _app = services.BuildServiceProvider();
        _players = _app.GetRequiredService<PlayerService>();
    }

    public void Dispose() => _app.Dispose();

    private ProfileService Profiles => _app.GetRequiredService<ProfileService>();

    [Fact]
    public async Task A_name_is_enough_to_add_a_player()
    {
        var added = await _players.AddAsync(new PlayerInput("  Sami "));

        var stored = Assert.Single(await _players.ListAsync());
        Assert.Equal(added.Id, stored.Id);
        Assert.Equal("Sami", stored.Name);
        Assert.Null(stored.PreferredPositionCode);
        Assert.Null(stored.Level);
        Assert.False(stored.IsArchived);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public async Task Level_bounds_are_accepted(int level)
    {
        var added = await _players.AddAsync(new PlayerInput("Sami", Level: level));

        Assert.Equal(level, (await _players.FindAsync(added.Id))!.Level);
    }

    [Theory]
    [InlineData("", null, null)]
    [InlineData("   ", null, null)]
    [InlineData("12345678901234567890123456789012345678901", null, null)]
    [InlineData("Sami", "inconnu", null)]
    [InlineData("Sami", null, 0)]
    [InlineData("Sami", null, 6)]
    public async Task Invalid_input_is_rejected_and_nothing_is_written(string name, string? position, int? level)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _players.AddAsync(new PlayerInput(name, position, level)));

        Assert.Empty(await _players.ListAsync());
    }

    [Fact]
    public async Task A_player_can_be_edited_and_the_level_removed()
    {
        var added = await _players.AddAsync(new PlayerInput("Sami", Level: 3));

        await _players.UpdateAsync(added.Id, new PlayerInput("Samir", "football.defender"));

        var stored = Assert.Single(await _players.ListAsync());
        Assert.Equal("Samir", stored.Name);
        Assert.Equal("football.defender", stored.PreferredPositionCode);
        Assert.Null(stored.Level);
    }

    [Fact]
    public async Task Two_players_may_share_a_name()
    {
        await _players.AddAsync(new PlayerInput("Sami"));
        await _players.AddAsync(new PlayerInput("Sami"));

        Assert.Equal(2, (await _players.ListAsync()).Count);
    }

    [Fact]
    public async Task An_archived_player_leaves_selections_but_remains_findable_and_can_come_back()
    {
        var sami = await _players.AddAsync(new PlayerInput("Sami"));
        await _players.AddAsync(new PlayerInput("Alice"));

        await _players.SetArchivedAsync(sami.Id, archived: true);

        Assert.Equal(new[] { "Alice" }, (await _players.ListSelectableAsync()).Select(p => p.Name));
        Assert.True((await _players.FindAsync(sami.Id))!.IsArchived);
        Assert.Equal(2, (await _players.ListAsync()).Count);

        await _players.SetArchivedAsync(sami.Id, archived: false);

        Assert.Equal(new[] { "Alice", "Sami" }, (await _players.ListSelectableAsync()).Select(p => p.Name));
    }

    [Fact]
    public async Task My_own_player_keeps_the_profile_name_and_cannot_be_archived()
    {
        await Profiles.SaveAsync(new ProfileInput("Sami", PreferredPositionCode: "football.forward"));
        var me = Assert.Single(await _players.ListAsync());

        await _players.UpdateAsync(me.Id, new PlayerInput("Autre", "football.goalkeeper", 4));

        var stored = (await _players.FindAsync(me.Id))!;
        Assert.Equal("Sami", stored.Name);
        Assert.Equal("football.forward", stored.PreferredPositionCode);
        Assert.Equal(4, stored.Level);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _players.SetArchivedAsync(me.Id, archived: true));
    }

    [Fact]
    public async Task Saving_the_profile_keeps_my_level()
    {
        await Profiles.SaveAsync(new ProfileInput("Sami"));
        var me = Assert.Single(await _players.ListAsync());
        await _players.UpdateAsync(me.Id, new PlayerInput("Sami", Level: 2));

        await Profiles.SaveAsync(new ProfileInput("Samir"));

        var stored = Assert.Single(await _players.ListAsync());
        Assert.Equal("Samir", stored.Name);
        Assert.Equal(2, stored.Level);
    }
}
