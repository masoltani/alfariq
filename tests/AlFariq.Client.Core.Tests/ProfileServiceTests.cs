using AlFariq.Application;
using AlFariq.Client.Core.Storage;
using AlFariq.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace AlFariq.Client.Core.Tests;

public class ProfileServiceTests
{
    private readonly InMemoryDocumentStore _store = new();

    /// <summary>Une application « neuve » sur le même stockage, comme après fermeture et réouverture.</summary>
    private ServiceProvider StartApp()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDocumentStore>(_store);
        services.AddSingleton<TimeProvider>(new FakeClock());
        services.AddAlFariqApplication();
        services.AddLocalRepository<Profile>(LocalSchema.ProfilesStore);
        services.AddLocalRepository<Player>(LocalSchema.PlayersStore);
        return services.BuildServiceProvider();
    }

    private static ProfileService ProfilesOf(ServiceProvider app) => app.GetRequiredService<ProfileService>();

    private static Task<IReadOnlyList<Player>> PlayersOf(ServiceProvider app) =>
        app.GetRequiredService<IRepository<Player>>().ListAsync();

    [Fact]
    public async Task There_is_no_profile_at_first_launch()
    {
        using var app = StartApp();

        Assert.Null(await ProfilesOf(app).GetAsync());
    }

    [Fact]
    public async Task A_nickname_is_enough_and_football_is_the_default_sport()
    {
        using var app = StartApp();

        var profile = await ProfilesOf(app).SaveAsync(new ProfileInput("  Sami  "));

        Assert.Equal("Sami", profile.Nickname);
        Assert.Equal(SportCatalog.FootballCode, profile.SportCode);
        Assert.Null(profile.PreferredPositionCode);
    }

    [Fact]
    public async Task Creating_the_profile_adds_me_to_the_players()
    {
        using var app = StartApp();

        var profile = await ProfilesOf(app).SaveAsync(
            new ProfileInput("Sami", PreferredPositionCode: "football.forward"));

        var me = Assert.Single(await PlayersOf(app));
        Assert.Equal(profile.Id, me.ProfileId);
        Assert.Equal("Sami", me.Name);
        Assert.Equal("football.forward", me.PreferredPositionCode);
    }

    [Fact]
    public async Task Saving_again_updates_the_same_profile_and_player()
    {
        using var app = StartApp();
        var first = await ProfilesOf(app).SaveAsync(new ProfileInput("Sami"));
        var playerId = Assert.Single(await PlayersOf(app)).Id;

        var second = await ProfilesOf(app).SaveAsync(new ProfileInput("Samir", "futsal", "futsal.pivot"));

        Assert.Equal(first.Id, second.Id);
        var me = Assert.Single(await PlayersOf(app));
        Assert.Equal(playerId, me.Id);
        Assert.Equal("Samir", me.Name);
        Assert.Equal("futsal.pivot", me.PreferredPositionCode);
    }

    [Fact]
    public async Task The_profile_survives_closing_the_app()
    {
        using (var app = StartApp())
        {
            await ProfilesOf(app).SaveAsync(new ProfileInput("Sami", "handball", "handball.back"));
        }

        using var reopened = StartApp();
        var profile = await ProfilesOf(reopened).GetAsync();

        Assert.NotNull(profile);
        Assert.Equal("Sami", profile.Nickname);
        Assert.Equal("handball", profile.SportCode);
        Assert.Equal("handball.back", profile.PreferredPositionCode);
    }

    [Fact]
    public async Task A_missing_own_player_is_restored_by_the_next_save()
    {
        using var app = StartApp();
        await ProfilesOf(app).SaveAsync(new ProfileInput("Sami"));
        var players = app.GetRequiredService<IRepository<Player>>();
        await players.DeleteAsync(Assert.Single(await players.ListAsync()).Id);

        await ProfilesOf(app).SaveAsync(new ProfileInput("Sami"));

        Assert.Single(await PlayersOf(app));
    }

    [Theory]
    [InlineData("", "football", null)]
    [InlineData("   ", "football", null)]
    [InlineData("12345678901234567890123456789012345678901", "football", null)]
    [InlineData("Sami", "inconnu", null)]
    [InlineData("Sami", "football", "futsal.pivot")]
    public async Task Invalid_input_is_rejected_and_nothing_is_written(string nickname, string sport, string? position)
    {
        using var app = StartApp();

        await Assert.ThrowsAsync<ArgumentException>(
            () => ProfilesOf(app).SaveAsync(new ProfileInput(nickname, sport, position)));

        Assert.Null(await ProfilesOf(app).GetAsync());
        Assert.Empty(await PlayersOf(app));
    }
}
