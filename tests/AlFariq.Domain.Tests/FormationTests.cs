using AlFariq.Domain;

namespace AlFariq.Domain.Tests;

public class FormationTests
{
    private const string Keeper = "football.goalkeeper";

    private static Formation Get(int playersPerSide, string code) =>
        FormationCatalog.Find("football", playersPerSide, code)!;

    private static string[] PositionsOf(Formation formation, IReadOnlyDictionary<Guid, int> placed, params Guid[] players) =>
        players.Select(player => formation.Slots[placed[player]].PositionCode).ToArray();

    [Theory]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(11)]
    public void Every_football_format_has_formations_of_the_right_size(int format)
    {
        var formations = FormationCatalog.For("football", format);

        Assert.NotEmpty(formations);
        Assert.Equal(formations.Count, formations.Select(formation => formation.Code).Distinct().Count());
        Assert.All(formations, formation =>
        {
            Assert.Equal(format, formation.PlayersPerSide);
            Assert.Equal(Enumerable.Range(0, format), formation.Slots.Select(slot => slot.Index));
            Assert.All(formation.Slots, slot => Assert.True(SportCatalog.Default.HasPosition(slot.PositionCode)));
            Assert.True(formation.Slots.Count(slot => slot.PositionCode == Keeper) <= 1);
        });
    }

    [Fact]
    public void The_formations_of_the_user_story_are_provided()
    {
        Assert.True(Get(11, "4-4-2").HasGoalkeeper);
        Assert.False(Get(5, "2-2-1").HasGoalkeeper);
    }

    [Fact]
    public void There_is_no_formation_for_another_sport_or_a_free_format()
    {
        Assert.Empty(FormationCatalog.For("futsal", 5));
        Assert.Empty(FormationCatalog.For("football", 6));
        Assert.Null(FormationCatalog.Find("football", 5, "4-4-2"));
        Assert.Null(FormationCatalog.Find("football", 5, null));
    }

    [Fact]
    public void Every_variant_belongs_to_a_general_position()
    {
        var positions = SportCatalog.Default.PositionCodes;

        Assert.All(positions, position => Assert.Contains(SportCatalog.BaseOf(position), positions));
        Assert.Equal(
            new[] { "football.goalkeeper", "football.defender", "football.midfielder", "football.forward" },
            positions.Select(SportCatalog.BaseOf).Distinct());
    }

    [Fact]
    public void A_variant_counts_as_its_general_position_for_balancing()
    {
        var player = new Player { Name = "Sami", PreferredPositionCode = "football.left_back" };

        Assert.Equal("football.defender", BalancePlayer.From(player, SportCatalog.Default).PositionCode);
    }

    [Fact]
    public void Players_go_to_their_exact_position_before_a_close_one()
    {
        var formation = Get(11, "4-4-2");
        var defender = Guid.CreateVersion7();
        var leftBack = Guid.CreateVersion7();

        var placed = formation.Distribute(
        [
            new SlotCandidate(defender, "football.defender", WasOnPitch: false),
            new SlotCandidate(leftBack, "football.left_back", WasOnPitch: false),
        ]);

        var positions = PositionsOf(formation, placed, leftBack, defender);
        Assert.Equal("football.left_back", positions[0]);
        Assert.Equal("football.defender", SportCatalog.BaseOf(positions[1]));
    }

    [Fact]
    public void Nobody_becomes_goalkeeper_automatically()
    {
        var formation = Get(5, "2-1-1");
        var midfielders = Enumerable.Range(0, 5).Select(_ => Guid.CreateVersion7()).ToList();

        var placed = formation.Distribute(
            midfielders.Select(id => new SlotCandidate(id, "football.midfielder", WasOnPitch: true)).ToList());

        Assert.Equal(4, placed.Count);
        Assert.DoesNotContain(0, placed.Values);
    }

    [Fact]
    public void A_player_without_position_who_was_not_on_the_pitch_stays_off()
    {
        var placed = Get(5, "2-1-1").Distribute([new SlotCandidate(Guid.CreateVersion7(), null, WasOnPitch: false)]);

        Assert.Empty(placed);
    }
}
