using System.Text.Json.Serialization;

namespace AlFariq.Domain;

/// <summary>État enregistré d'un match, stocké en texte (« En cours » se déduit de l'heure, il n'est pas saisi).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MatchStatus>))]
public enum MatchStatus
{
    Planned,

    /// <summary>Un score est validé (US-012).</summary>
    Finished,

    Cancelled,
}

/// <summary>Une rencontre (US-006).</summary>
public sealed class Match : Entity
{
    public const int MinPlayersPerSide = 1;
    public const int MaxPlayersPerSide = 30;
    public const int MinDurationMinutes = 10;
    public const int MaxDurationMinutes = 300;
    public const int DefaultDurationMinutes = 60;
    public const int MaxLocationLength = 100;
    public const int MaxNotesLength = 500;
    public const int MaxCancellationReasonLength = 200;
    public const int MinYear = 2000;
    public const int MaxYear = 2100;

    /// <summary>Début, avec le décalage UTC du lieu de saisie.</summary>
    public DateTimeOffset StartsAt { get; set; }

    public int DurationMinutes { get; set; } = DefaultDurationMinutes;

    /// <summary>Code de <see cref="SportCatalog"/>.</summary>
    public string SportCode { get; set; } = SportCatalog.FootballCode;

    /// <summary>Format : nombre de joueurs par équipe.</summary>
    public int PlayersPerSide { get; set; }

    public string? Location { get; set; }

    public string? Notes { get; set; }

    public MatchStatus Status { get; set; } = MatchStatus.Planned;

    /// <summary>Motif facultatif d'un match annulé ; null dans les autres états.</summary>
    public string? CancellationReason { get; set; }

    /// <summary>Nombre de joueurs attendus : les deux équipes au format du match.</summary>
    [JsonIgnore]
    public int ExpectedPlayers => 2 * PlayersPerSide;

    [JsonIgnore]
    public DateTimeOffset EndsAt => StartsAt.AddMinutes(DurationMinutes);

    /// <summary>Vrai une fois l'heure de fin atteinte.</summary>
    public bool IsOver(DateTimeOffset now) => EndsAt <= now;

    /// <summary>
    /// Un match passé : terminé (même si le score est saisi avant la fin prévue) ou à l'heure de fin dépassée. Un
    /// match en cours n'est pas encore passé.
    /// </summary>
    public bool IsPast(DateTimeOffset now) => Status == MatchStatus.Finished || IsOver(now);
}
