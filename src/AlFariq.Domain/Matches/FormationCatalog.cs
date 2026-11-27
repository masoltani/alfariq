namespace AlFariq.Domain;

/// <summary>
/// Formations livrées avec l'application (US-010), pour le football seulement. Pour en ajouter une : l'ajouter ici.
/// Ne jamais renommer une formation livrée ni réordonner ses emplacements : les compositions enregistrées s'y réfèrent.
/// </summary>
public static class FormationCatalog
{
    private const string Goal = "football.goalkeeper";
    private const string Defender = "football.defender";
    private const string CentreBack = "football.centre_back";
    private const string LeftBack = "football.left_back";
    private const string RightBack = "football.right_back";
    private const string Midfielder = "football.midfielder";
    private const string Holding = "football.defensive_midfielder";
    private const string Central = "football.central_midfielder";
    private const string Attacking = "football.attacking_midfielder";
    private const string LeftMid = "football.left_midfielder";
    private const string RightMid = "football.right_midfielder";
    private const string Forward = "football.forward";
    private const string Striker = "football.striker";
    private const string LeftWing = "football.left_winger";
    private const string RightWing = "football.right_winger";

    private static readonly IReadOnlyList<Formation> Football =
    [
        // 5 contre 5. « 2-2-1 » compte cinq joueurs de champ : c'est la variante sans gardien attitré.
        Create("2-1-1", [Goal], [Defender, Defender], [Midfielder], [Forward]),
        Create("1-2-1", [Goal], [Defender], [LeftMid, RightMid], [Forward]),
        Create("2-2-1", [Defender, Defender], [Midfielder, Midfielder], [Forward]),

        // 7 contre 7.
        Create("2-3-1", [Goal], [CentreBack, CentreBack], [LeftMid, Central, RightMid], [Striker]),
        Create("3-2-1", [Goal], [LeftBack, CentreBack, RightBack], [Central, Central], [Striker]),
        Create("2-2-2", [Goal], [CentreBack, CentreBack], [Central, Central], [Striker, Striker]),

        // 11 contre 11.
        Create(
            "4-4-2",
            [Goal],
            [LeftBack, CentreBack, CentreBack, RightBack],
            [LeftMid, Central, Central, RightMid],
            [Striker, Striker]),
        Create(
            "4-3-3",
            [Goal],
            [LeftBack, CentreBack, CentreBack, RightBack],
            [Central, Holding, Central],
            [LeftWing, Striker, RightWing]),
        Create(
            "4-2-3-1",
            [Goal],
            [LeftBack, CentreBack, CentreBack, RightBack],
            [Holding, Holding],
            [LeftMid, Attacking, RightMid],
            [Striker]),
        Create(
            "3-5-2",
            [Goal],
            [CentreBack, CentreBack, CentreBack],
            [LeftMid, Central, Holding, Central, RightMid],
            [Striker, Striker]),
    ];

    /// <summary>Les formations d'un format ; vide s'il n'y en a pas (autre sport ou format libre).</summary>
    public static IReadOnlyList<Formation> For(string sportCode, int playersPerSide) =>
        sportCode == SportCatalog.FootballCode
            ? Football.Where(formation => formation.PlayersPerSide == playersPerSide).ToList()
            : [];

    /// <summary>La formation portant ce code pour ce format, ou null.</summary>
    public static Formation? Find(string sportCode, int playersPerSide, string? code) =>
        For(sportCode, playersPerSide).FirstOrDefault(formation => formation.Code == code);

    private static Formation Create(string code, params string[][] lines)
    {
        var index = 0;
        var slots = lines
            .Select(line => (IReadOnlyList<FormationSlot>)line
                .Select(position => new FormationSlot(index++, position))
                .ToList())
            .ToList();
        return new Formation(code, slots);
    }
}
