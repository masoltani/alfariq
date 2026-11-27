namespace AlFariq.Domain;

/// <summary>
/// Répartit des joueurs en deux équipes équilibrées (US-009, RG-EQU-01 à 07). Calcul exact, local et sans réseau.
/// </summary>
/// <remarks>
/// <para>Contraintes : les effectifs diffèrent d'au plus un joueur et les joueurs verrouillés gardent leur côté.</para>
/// <para>
/// Priorités : d'abord le plus petit écart de niveau total ; ensuite, à écart égal, éviter que tous les joueurs
/// d'un même poste soient dans la même équipe, les gardiens avant les autres postes.
/// </para>
/// <para>
/// Méthode : programmation dynamique sur (joueurs placés, effectif de A, niveau de A, poste en cours). Les joueurs
/// sont parcourus poste par poste, ce qui évite d'avoir à retenir la répartition des postes déjà traités.
/// Les niveaux étant entiers et leur total fixe, deux répartitions ont des écarts de même parité : avec la
/// tolérance par défaut (1), une autre proposition a donc exactement le même écart.
/// </para>
/// <para>Une instance correspond à un jeu de verrous et mémorise les propositions déjà faites.</para>
/// </remarks>
public sealed class TeamBalancer
{
    /// <summary>Niveau compté pour un joueur qui n'en a pas (RG-EQU-03).</summary>
    public const int DefaultLevel = 3;

    /// <summary>Écart de niveau supplémentaire admis pour une autre proposition (RG-EQU-05).</summary>
    public const int DefaultTolerance = 1;

    // Séparer les gardiens compte plus que séparer tous les autres postes réunis.
    private const int GoalkeeperWeight = 16;

    private const byte Unknown = byte.MaxValue;
    private const byte Impossible = byte.MaxValue - 1;

    private const int OnA = 1;
    private const int OnB = 2;
    private const int BothSides = OnA | OnB;

    private static readonly TeamSide[] OnlyA = [TeamSide.A];
    private static readonly TeamSide[] OnlyB = [TeamSide.B];
    private static readonly TeamSide[] AOrB = [TeamSide.A, TeamSide.B];

    private readonly Slot[] _slots;
    private readonly Random _random;
    private readonly int _totalLevel;
    private readonly int _maxExcess;
    private readonly bool _mirrorsAreEqual;
    private readonly HashSet<string> _seen = [];
    private readonly Solver? _best;
    private readonly int _bestPenalty;
    private Solver? _equivalent;

    /// <param name="players">Les joueurs à répartir.</param>
    /// <param name="random">Hasard qui choisit parmi les répartitions de même qualité.</param>
    /// <param name="tolerance">Écart de niveau supplémentaire admis pour une autre proposition.</param>
    /// <exception cref="ArgumentException">Un joueur figure deux fois.</exception>
    public TeamBalancer(IReadOnlyList<BalancePlayer> players, Random random, int tolerance = DefaultTolerance)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(random);
        ArgumentOutOfRangeException.ThrowIfNegative(tolerance);
        if (players.Select(player => player.Id).Distinct().Count() != players.Count)
        {
            throw new ArgumentException("Un joueur ne peut figurer qu'une fois.", nameof(players));
        }

        _random = random;
        Tolerance = tolerance;
        MaxPerSide = (players.Count + 1) / 2;

        // Sans aucun verrou, échanger les deux équipes donne la même répartition.
        _mirrorsAreEqual = players.All(player => player.LockedSide is null);
        _slots = BuildSlots(players);
        _totalLevel = players.Sum(player => LevelOf(player));
        _maxExcess = Math.Min(
            _slots.Sum(slot => slot.Excess), (Player.MaxLevel - Player.MinLevel) * MaxPerSide);

        // Pour que chaque répartition ne soit comptée qu'une fois, le premier joueur est alors toujours en A.
        if (_mirrorsAreEqual && _slots.Length > 0)
        {
            _slots[0] = _slots[0] with { Fixed = TeamSide.A };
        }

        if (FindMinimumGap() is { } gap)
        {
            MinimumGap = gap;
            _best = new Solver(this, gap);
            _bestPenalty = _best.Cost(0, 0, 0, 0);
            IsPossible = true;
        }
    }

    public int Tolerance { get; }

    /// <summary>Effectif maximal d'une équipe : la moitié des joueurs, arrondie au-dessus.</summary>
    public int MaxPerSide { get; }

    /// <summary>Faux quand les verrous placent plus de <see cref="MaxPerSide"/> joueurs d'un même côté.</summary>
    public bool IsPossible { get; }

    /// <summary>Le plus petit écart de niveau total possible avec ces verrous.</summary>
    public int MinimumGap { get; }

    /// <summary>Vrai si cette répartition respecte les verrous et reste d'une qualité équivalente à la meilleure.</summary>
    public bool Accepts(IReadOnlyDictionary<Guid, TeamSide> sides)
    {
        ArgumentNullException.ThrowIfNull(sides);
        if (!IsPossible || sides.Count != _slots.Length)
        {
            return false;
        }

        var countA = 0;
        var excessA = 0;
        var flags = 0;
        var penalty = 0;
        for (var index = 0; index < _slots.Length; index++)
        {
            var slot = _slots[index];
            if (!sides.TryGetValue(slot.Id, out var side) || (slot.Locked is { } locked && locked != side))
            {
                return false;
            }

            if (side == TeamSide.A)
            {
                countA++;
                excessA += slot.Excess;
                flags |= OnA;
            }
            else
            {
                flags |= OnB;
            }

            if (slot.EndsGroup)
            {
                penalty += flags == BothSides ? 0 : slot.GroupWeight;
                flags = 0;
            }
        }

        return countA <= MaxPerSide
            && _slots.Length - countA <= MaxPerSide
            && GapOf(countA, excessA) <= MinimumGap + Tolerance
            && penalty <= _bestPenalty;
    }

    /// <summary>
    /// Sans <paramref name="current"/> : une des meilleures répartitions. Avec <paramref name="current"/> : une
    /// répartition différente de celle-là et de qualité équivalente, pas encore proposée ; quand toutes l'ont été,
    /// le tour recommence.
    /// </summary>
    /// <returns>
    /// Null si les verrous rendent la répartition impossible, ou s'il n'existe aucune autre répartition équivalente.
    /// </returns>
    public BalanceProposal? Propose(IReadOnlyDictionary<Guid, TeamSide>? current = null)
    {
        if (_best is null)
        {
            return null;
        }

        var solver = current is null ? _best : (_equivalent ??= new Solver(this, MinimumGap + Tolerance));
        var currentKey = current is null ? null : KeyOf(current);
        if (currentKey is not null)
        {
            _seen.Add(currentKey);
        }

        var proposal = Pick(solver);
        if (proposal is null)
        {
            // Tout a déjà été proposé : on oublie, sauf la répartition affichée.
            _seen.Clear();
            if (currentKey is not null)
            {
                _seen.Add(currentKey);
            }

            proposal = Pick(solver);
        }

        return proposal;
    }

    private static int LevelOf(BalancePlayer player) =>
        Math.Clamp(player.Level ?? DefaultLevel, Player.MinLevel, Player.MaxLevel);

    /// <summary>Range les joueurs poste par poste et note ce que coûte un poste regroupé d'un seul côté.</summary>
    private static Slot[] BuildSlots(IReadOnlyList<BalancePlayer> players)
    {
        var slots = new List<Slot>(players.Count);
        foreach (var group in players
            .GroupBy(player => player.PositionCode ?? "")
            .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var members = group.ToList();
            var weight = group.Key.Length == 0 || members.Count < 2 ? 0
                : group.Key.EndsWith(".goalkeeper", StringComparison.Ordinal) ? GoalkeeperWeight
                : 1;

            for (var index = 0; index < members.Count; index++)
            {
                var player = members[index];
                slots.Add(new Slot(
                    player.Id,
                    LevelOf(player) - Player.MinLevel,
                    group.Key,
                    weight,
                    EndsGroup: index == members.Count - 1,
                    Locked: player.LockedSide,
                    Fixed: player.LockedSide));
            }
        }

        return slots.ToArray();
    }

    // L'« excédent » est le niveau au-delà du minimum : il garde les tables petites.
    private int GapOf(int countA, int excessA) => Math.Abs((2 * (excessA + (countA * Player.MinLevel))) - _totalLevel);

    /// <summary>Le plus petit écart parmi tous les couples (effectif, niveau) que l'équipe A peut atteindre.</summary>
    private int? FindMinimumGap()
    {
        var reachable = new bool[MaxPerSide + 1, _maxExcess + 1];
        reachable[0, 0] = true;

        for (var index = 0; index < _slots.Length; index++)
        {
            var next = new bool[MaxPerSide + 1, _maxExcess + 1];
            for (var countA = 0; countA <= Math.Min(index, MaxPerSide); countA++)
            {
                for (var excessA = 0; excessA <= _maxExcess; excessA++)
                {
                    if (!reachable[countA, excessA])
                    {
                        continue;
                    }

                    foreach (var side in ChoicesAt(index))
                    {
                        if (TryPlace(index, countA, excessA, side, out var newCount, out var newExcess))
                        {
                            next[newCount, newExcess] = true;
                        }
                    }
                }
            }

            reachable = next;
        }

        int? minimum = null;
        for (var countA = 0; countA <= MaxPerSide; countA++)
        {
            for (var excessA = 0; excessA <= _maxExcess; excessA++)
            {
                if (reachable[countA, excessA] && (minimum is null || GapOf(countA, excessA) < minimum))
                {
                    minimum = GapOf(countA, excessA);
                }
            }
        }

        return minimum;
    }

    private TeamSide[] ChoicesAt(int index) => _slots[index].Fixed switch
    {
        TeamSide.A => OnlyA,
        TeamSide.B => OnlyB,
        _ => AOrB,
    };

    /// <summary>Ajoute le joueur au côté demandé ; faux si ce côté est complet.</summary>
    private bool TryPlace(int index, int countA, int excessA, TeamSide side, out int newCount, out int newExcess)
    {
        var onA = side == TeamSide.A;
        newCount = onA ? countA + 1 : countA;
        newExcess = onA ? excessA + _slots[index].Excess : excessA;

        return newCount <= MaxPerSide
            && index + 1 - newCount <= MaxPerSide
            && newExcess <= _maxExcess;
    }

    /// <summary>Le poste en cours : côtés déjà occupés après ce joueur, et pénalité si le poste se termine.</summary>
    private (int Flags, int Penalty) Follow(int index, int flags, TeamSide side)
    {
        var slot = _slots[index];
        var occupied = flags | (side == TeamSide.A ? OnA : OnB);

        return slot.EndsGroup
            ? (0, occupied == BothSides ? 0 : slot.GroupWeight)
            : (occupied, 0);
    }

    /// <summary>Tire au hasard une répartition de cette qualité qui n'a pas encore été proposée.</summary>
    private BalanceProposal? Pick(Solver solver)
    {
        var sides = new TeamSide[_slots.Length];
        if (!Walk(0, 0, 0, 0, 0))
        {
            return null;
        }

        _seen.Add(Key(sides));
        return Describe(sides);

        // Parcours en profondeur, dans un ordre aléatoire, sans entrer dans les branches qui ne mènent à rien.
        bool Walk(int index, int countA, int excessA, int flags, int penalty)
        {
            if (index == _slots.Length)
            {
                return !_seen.Contains(Key(sides));
            }

            var choices = ChoicesAt(index);
            var first = _random.Next(choices.Length);
            for (var offset = 0; offset < choices.Length; offset++)
            {
                var side = choices[(first + offset) % choices.Length];
                if (!TryPlace(index, countA, excessA, side, out var newCount, out var newExcess))
                {
                    continue;
                }

                var (newFlags, cost) = Follow(index, flags, side);
                var rest = solver.Cost(index + 1, newCount, newExcess, newFlags);
                if (rest == Impossible || penalty + cost + rest > _bestPenalty)
                {
                    continue;
                }

                sides[index] = side;
                if (Walk(index + 1, newCount, newExcess, newFlags, penalty + cost))
                {
                    return true;
                }
            }

            return false;
        }
    }

    private static string Key(IEnumerable<TeamSide> sides) =>
        string.Concat(sides.Select(side => side == TeamSide.A ? 'A' : 'B'));

    /// <summary>La clé d'une répartition venue de l'extérieur ; null si elle ne porte pas sur les mêmes joueurs.</summary>
    private string? KeyOf(IReadOnlyDictionary<Guid, TeamSide> sides)
    {
        if (sides.Count != _slots.Length || !_slots.All(slot => sides.ContainsKey(slot.Id)))
        {
            return null;
        }

        var ordered = _slots.Select(slot => sides[slot.Id]).ToList();
        if (_mirrorsAreEqual && ordered.Count > 0 && ordered[0] == TeamSide.B)
        {
            ordered = ordered.Select(side => side == TeamSide.A ? TeamSide.B : TeamSide.A).ToList();
        }

        return Key(ordered);
    }

    private BalanceProposal Describe(TeamSide[] sides)
    {
        var assignments = new Dictionary<Guid, TeamSide>(_slots.Length);
        var grouped = new List<string>();
        var levelA = 0;
        var flags = 0;

        for (var index = 0; index < _slots.Length; index++)
        {
            var slot = _slots[index];
            assignments[slot.Id] = sides[index];
            flags |= sides[index] == TeamSide.A ? OnA : OnB;
            if (sides[index] == TeamSide.A)
            {
                levelA += slot.Excess + Player.MinLevel;
            }

            if (slot.EndsGroup)
            {
                if (slot.GroupWeight > 0 && flags != BothSides)
                {
                    grouped.Add(slot.Position);
                }

                flags = 0;
            }
        }

        return new BalanceProposal(assignments, levelA, _totalLevel - levelA, grouped);
    }

    private readonly record struct Slot(
        Guid Id, int Excess, string Position, int GroupWeight, bool EndsGroup, TeamSide? Locked, TeamSide? Fixed);

    /// <summary>
    /// Pour un écart maximal donné : la plus petite pénalité de postes qu'on peut encore obtenir depuis un état.
    /// </summary>
    private sealed class Solver
    {
        private readonly TeamBalancer _owner;
        private readonly int _maxGap;
        private readonly byte[] _memo;

        public Solver(TeamBalancer owner, int maxGap)
        {
            _owner = owner;
            _maxGap = maxGap;
            _memo = new byte[(owner._slots.Length + 1) * (owner.MaxPerSide + 1) * (owner._maxExcess + 1) * 4];
            Array.Fill(_memo, Unknown);
        }

        /// <returns><see cref="Impossible"/> si aucune répartition ne respecte l'écart depuis cet état.</returns>
        public int Cost(int index, int countA, int excessA, int flags)
        {
            var key = (((((index * (_owner.MaxPerSide + 1)) + countA) * (_owner._maxExcess + 1)) + excessA) * 4) + flags;
            if (_memo[key] != Unknown)
            {
                return _memo[key];
            }

            int best = Impossible;
            if (index == _owner._slots.Length)
            {
                if (_owner.GapOf(countA, excessA) <= _maxGap)
                {
                    best = 0;
                }
            }
            else
            {
                foreach (var side in _owner.ChoicesAt(index))
                {
                    if (!_owner.TryPlace(index, countA, excessA, side, out var newCount, out var newExcess))
                    {
                        continue;
                    }

                    var (newFlags, penalty) = _owner.Follow(index, flags, side);
                    var rest = Cost(index + 1, newCount, newExcess, newFlags);
                    if (rest != Impossible)
                    {
                        best = Math.Min(best, rest + penalty);
                    }
                }
            }

            _memo[key] = (byte)best;
            return best;
        }
    }
}
