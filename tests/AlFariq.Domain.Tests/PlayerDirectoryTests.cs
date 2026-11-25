using System.Diagnostics;
using AlFariq.Domain;

namespace AlFariq.Domain.Tests;

public class PlayerDirectoryTests
{
    private static Player[] Players(params string[] names) => names.Select(name => new Player { Name = name }).ToArray();

    private static IEnumerable<string> Names(IEnumerable<Player> players) => players.Select(player => player.Name);

    [Fact]
    public void Without_a_query_all_players_are_sorted_by_name_ignoring_case_and_accents()
    {
        var players = Players("farid", "Élodie", "Dan", "Zoé", "alice");

        var result = PlayerDirectory.Search(players, null);

        Assert.Equal(new[] { "alice", "Dan", "Élodie", "farid", "Zoé" }, Names(result));
    }

    [Theory]
    [InlineData("elo")]
    [InlineData("ÉLODIE")]
    [InlineData("  lodie ")]
    public void Search_matches_part_of_the_name_ignoring_case_and_accents(string query)
    {
        var players = Players("Élodie", "Sami");

        Assert.Equal(new[] { "Élodie" }, Names(PlayerDirectory.Search(players, query)));
    }

    [Fact]
    public void Search_can_find_nobody()
    {
        Assert.Empty(PlayerDirectory.Search(Players("Sami"), "zzz"));
    }

    [Theory]
    [InlineData("elodie martin", true)]
    [InlineData("  ÉLODIE   Martin ", true)]
    [InlineData("Élodie", false)]
    [InlineData("", false)]
    public void Same_name_ignores_case_accents_and_extra_spaces(string name, bool expected)
    {
        Assert.Equal(expected, PlayerDirectory.HasSameName(Players("Élodie Martin"), name));
    }

    [Fact]
    public void A_player_is_not_a_duplicate_of_itself()
    {
        var players = Players("Sami");

        Assert.False(PlayerDirectory.HasSameName(players, "Sami", players[0].Id));
    }

    [Fact]
    public void CleanName_trims_and_collapses_spaces()
    {
        Assert.Equal("Jean Michel", PlayerDirectory.CleanName("  Jean \t  Michel  "));
        Assert.Equal("", PlayerDirectory.CleanName(null));
    }

    [Fact]
    public void Search_and_sort_stay_quick_with_many_players()
    {
        // Budget volontairement large (machines de CI) : il ne détecte qu'une régression grossière.
        var players = Enumerable.Range(0, 1000).Select(i => new Player { Name = $"Joueur {(i * 7919) % 1000:D4}" }).ToList();
        var watch = Stopwatch.StartNew();

        var all = PlayerDirectory.Search(players, "");
        var some = PlayerDirectory.Search(players, "joueur 09");

        watch.Stop();
        Assert.Equal(1000, all.Count);
        Assert.Equal(100, some.Count);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(1), $"Durée : {watch.Elapsed}");
    }
}
