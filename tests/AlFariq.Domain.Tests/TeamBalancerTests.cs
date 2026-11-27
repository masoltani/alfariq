using AlFariq.Domain;

namespace AlFariq.Domain.Tests;

public class TeamBalancerTests
{
    private const string Keeper = "football.goalkeeper";
    private const string Forward = "football.forward";

    private static BalancePlayer Candidate(int? level, string? position = null, TeamSide? locked = null) =>
        new(Guid.CreateVersion7(), level, position, locked);

    private static List<BalancePlayer> Levels(params int[] levels) => levels.Select(level => Candidate(level)).ToList();

    private static TeamBalancer Balancer(IReadOnlyList<BalancePlayer> players, int seed = 1) =>
        new(players, new Random(seed));

    private static int CountOn(BalanceProposal proposal, TeamSide side) =>
        proposal.Sides.Values.Count(value => value == side);

    /// <summary>La répartition vue comme deux groupes, sans tenir compte du nom des équipes.</summary>
    private static string Groups(BalanceProposal proposal)
    {
        var teams = proposal.Sides
            .GroupBy(pair => pair.Value)
            .Select(team => string.Join(",", team.Select(pair => pair.Key).Order()));
        return string.Join("|", teams.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void The_level_gap_is_as_small_as_possible()
    {
        // Prendre les meilleurs à tour de rôle donnerait 9 contre 7 ; 5+3 contre 4+4 fait mieux.
        var players = Levels(5, 4, 4, 3);

        var proposal = Balancer(players).Propose()!;

        Assert.Equal(0, proposal.Gap);
        Assert.Equal(8, proposal.LevelA);
        Assert.Equal(8, proposal.LevelB);
        Assert.Equal(proposal.Sides[players[1].Id], proposal.Sides[players[2].Id]);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(7)]
    [InlineData(22)]
    [InlineData(61)]
    public void Team_sizes_differ_by_at_most_one(int count)
    {
        var players = Enumerable.Range(0, count).Select(index => Candidate(1 + (index % 5))).ToList();

        var proposal = Balancer(players).Propose()!;

        Assert.Equal(count, proposal.Sides.Count);
        Assert.InRange(Math.Abs(CountOn(proposal, TeamSide.A) - CountOn(proposal, TeamSide.B)), 0, 1);
    }

    [Fact]
    public void Sizes_come_before_levels()
    {
        // 5 contre 1+1+1 serait plus équilibré, mais les effectifs différeraient de deux joueurs.
        var proposal = Balancer(Levels(5, 1, 1, 1)).Propose()!;

        Assert.Equal(2, CountOn(proposal, TeamSide.A));
        Assert.Equal(4, proposal.Gap);
    }

    [Fact]
    public void A_player_without_level_counts_as_three()
    {
        var players = new List<BalancePlayer> { Candidate(null), Candidate(3), Candidate(5), Candidate(1) };

        var proposal = Balancer(players).Propose()!;

        Assert.Equal(3, TeamBalancer.DefaultLevel);
        Assert.Equal(12, proposal.LevelA + proposal.LevelB);
        Assert.Equal(0, proposal.Gap);
        Assert.Equal(proposal.Sides[players[0].Id], proposal.Sides[players[1].Id]);
    }

    [Fact]
    public void Locked_players_keep_their_side()
    {
        var players = new List<BalancePlayer>
        {
            Candidate(5, locked: TeamSide.B), Candidate(5, locked: TeamSide.B), Candidate(1), Candidate(1), Candidate(3), Candidate(3),
        };
        var balancer = Balancer(players);

        var proposal = balancer.Propose()!;

        for (var attempt = 0; attempt < 20; attempt++)
        {
            Assert.Equal(TeamSide.B, proposal.Sides[players[0].Id]);
            Assert.Equal(TeamSide.B, proposal.Sides[players[1].Id]);
            Assert.Equal(3, CountOn(proposal, TeamSide.B));
            Assert.Equal(4, proposal.Gap);
            proposal = balancer.Propose(proposal.Sides) ?? proposal;
        }
    }

    [Fact]
    public void Too_many_locks_on_one_side_give_no_proposal()
    {
        var players = new List<BalancePlayer>
        {
            Candidate(3, locked: TeamSide.A), Candidate(3, locked: TeamSide.A), Candidate(3, locked: TeamSide.A), Candidate(3),
        };

        var balancer = Balancer(players);

        Assert.False(balancer.IsPossible);
        Assert.Equal(2, balancer.MaxPerSide);
        Assert.Null(balancer.Propose());
    }

    [Fact]
    public void Another_proposal_is_different_and_as_good_until_all_have_been_shown()
    {
        // Six joueurs de même niveau : C(6,3) / 2 = 10 répartitions possibles.
        var balancer = Balancer(Levels(3, 3, 3, 3, 3, 3));
        var proposal = balancer.Propose()!;
        var seen = new HashSet<string> { Groups(proposal) };

        for (var attempt = 1; attempt < 10; attempt++)
        {
            proposal = balancer.Propose(proposal.Sides)!;

            Assert.Equal(0, proposal.Gap);
            Assert.True(seen.Add(Groups(proposal)), "Répartition déjà proposée.");
        }

        // Le tour recommence, sans reproposer celle qui est affichée.
        var next = balancer.Propose(proposal.Sides)!;

        Assert.NotEqual(Groups(proposal), Groups(next));
        Assert.Contains(Groups(next), seen.ToList());
    }

    [Fact]
    public void Swapping_the_two_teams_is_not_another_proposal()
    {
        var balancer = Balancer(Levels(5, 1));
        var proposal = balancer.Propose()!;

        Assert.Null(balancer.Propose(proposal.Sides));

        var mirrored = proposal.Sides.ToDictionary(
            pair => pair.Key, pair => pair.Value == TeamSide.A ? TeamSide.B : TeamSide.A);
        Assert.Null(balancer.Propose(mirrored));
        Assert.True(balancer.Accepts(mirrored));
        Assert.NotNull(balancer.Propose());
    }

    [Fact]
    public void Goalkeepers_and_forwards_are_shared_between_the_teams()
    {
        var players = new List<BalancePlayer>
        {
            Candidate(3, Keeper), Candidate(3, Keeper), Candidate(3, Forward), Candidate(3, Forward), Candidate(3), Candidate(3),
        };
        var balancer = Balancer(players);
        var proposal = balancer.Propose()!;

        for (var attempt = 0; attempt < 20; attempt++)
        {
            Assert.NotEqual(proposal.Sides[players[0].Id], proposal.Sides[players[1].Id]);
            Assert.NotEqual(proposal.Sides[players[2].Id], proposal.Sides[players[3].Id]);
            Assert.Empty(proposal.GroupedPositions);
            proposal = balancer.Propose(proposal.Sides)!;
        }
    }

    [Fact]
    public void The_level_gap_comes_before_positions()
    {
        // Les deux gardiens (5 et 1) doivent être ensemble pour que les équipes soient à égalité (6 contre 6).
        var players = new List<BalancePlayer> { Candidate(5, Keeper), Candidate(1, Keeper), Candidate(3), Candidate(3) };

        var proposal = Balancer(players).Propose()!;

        Assert.Equal(0, proposal.Gap);
        Assert.Equal(new[] { Keeper }, proposal.GroupedPositions);
    }

    [Fact]
    public void Goalkeepers_are_separated_before_other_positions()
    {
        // Une seule place reste en A : il faut réunir en B soit les gardiens, soit les attaquants.
        var players = new List<BalancePlayer>
        {
            Candidate(3, locked: TeamSide.A), Candidate(3, locked: TeamSide.A),
            Candidate(3, Keeper), Candidate(3, Keeper), Candidate(3, Forward), Candidate(3, Forward),
        };

        var proposal = Balancer(players).Propose()!;

        Assert.NotEqual(proposal.Sides[players[2].Id], proposal.Sides[players[3].Id]);
        Assert.Equal(new[] { Forward }, proposal.GroupedPositions);
    }

    [Fact]
    public void A_proposal_is_accepted_only_while_it_fits_the_locks()
    {
        var players = Levels(5, 4, 4, 3);
        var proposal = Balancer(players).Propose()!;
        var first = players[0].Id;

        List<BalancePlayer> Locked(TeamSide side) =>
            players.Select(player => player.Id == first ? player with { LockedSide = side } : player).ToList();

        var other = proposal.Sides[first] == TeamSide.A ? TeamSide.B : TeamSide.A;

        Assert.True(Balancer(Locked(proposal.Sides[first])).Accepts(proposal.Sides));
        Assert.False(Balancer(Locked(other)).Accepts(proposal.Sides));
        Assert.False(Balancer(Levels(5, 4)).Accepts(proposal.Sides));
    }

    [Fact]
    public void Results_match_an_exhaustive_search()
    {
        var random = new Random(2026);
        string?[] positions = [null, Keeper, Forward, "football.defender"];
        TeamSide?[] locks = [null, null, null, null, TeamSide.A, TeamSide.B];

        for (var round = 0; round < 300; round++)
        {
            var players = new List<BalancePlayer>();
            for (var count = random.Next(2, 10); players.Count < count;)
            {
                var level = random.Next(0, 6);
                players.Add(Candidate(
                    level == 0 ? null : level,
                    positions[random.Next(positions.Length)],
                    locks[random.Next(locks.Length)]));
            }
            var expected = ExhaustiveSearch(players);
            var balancer = Balancer(players, round);

            Assert.Equal(expected.Count > 0, balancer.IsPossible);
            if (expected.Count == 0)
            {
                continue;
            }

            var proposal = balancer.Propose()!;
            var found = new HashSet<string> { Groups(proposal) };
            Assert.Contains(Groups(proposal), expected.ToList());
            while (balancer.Propose(proposal.Sides) is { } next && found.Add(Groups(next)))
            {
                proposal = next;
            }

            Assert.Equal(expected.Order(StringComparer.Ordinal).ToList(), found.Order(StringComparer.Ordinal).ToList());
        }
    }

    [Fact]
    public void A_player_cannot_appear_twice()
    {
        var player = Candidate(3);

        Assert.Throws<ArgumentException>(() => Balancer([player, player]));
    }

    [Fact]
    public void A_position_of_another_sport_is_ignored()
    {
        var player = new Player { Name = "Sami", Level = 4, PreferredPositionCode = Keeper };

        Assert.Equal(Keeper, BalancePlayer.From(player, SportCatalog.Find("football")).PositionCode);
        Assert.Null(BalancePlayer.From(player, SportCatalog.Find("basketball")).PositionCode);
        Assert.Null(BalancePlayer.From(player, sport: null).PositionCode);
    }

    /// <summary>Toutes les meilleures répartitions, trouvées en les essayant toutes.</summary>
    private static HashSet<string> ExhaustiveSearch(IReadOnlyList<BalancePlayer> players)
    {
        var candidates = new List<(int Gap, int Penalty, string Groups)>();
        for (var mask = 0; mask < 1 << players.Count; mask++)
        {
            var sides = players
                .Select((player, index) => (player, Side: (mask & (1 << index)) != 0 ? TeamSide.A : TeamSide.B))
                .ToList();
            var countA = sides.Count(entry => entry.Side == TeamSide.A);
            if (Math.Abs((2 * countA) - players.Count) > 1
                || sides.Any(entry => entry.player.LockedSide is { } locked && locked != entry.Side))
            {
                continue;
            }

            var gap = Math.Abs(sides.Sum(entry =>
                (entry.Side == TeamSide.A ? 1 : -1) * (entry.player.Level ?? TeamBalancer.DefaultLevel)));
            var penalty = sides
                .Where(entry => entry.player.PositionCode is not null)
                .GroupBy(entry => entry.player.PositionCode)
                .Where(group => group.Count() > 1 && group.Select(entry => entry.Side).Distinct().Count() == 1)
                .Sum(group => group.Key == Keeper ? 16 : 1);
            var proposal = new BalanceProposal(sides.ToDictionary(e => e.player.Id, e => e.Side), 0, 0, []);
            candidates.Add((gap, penalty, Groups(proposal)));
        }

        if (candidates.Count == 0)
        {
            return [];
        }

        var best = candidates.Min(candidate => (candidate.Gap, candidate.Penalty));
        return candidates
            .Where(candidate => (candidate.Gap, candidate.Penalty) == best)
            .Select(candidate => candidate.Groups)
            .ToHashSet();
    }
}
