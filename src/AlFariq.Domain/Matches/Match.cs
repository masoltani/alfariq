using System.Text.Json.Serialization;

namespace AlFariq.Domain;

/// <summary>État enregistré d'un match, stocké en texte. US-012 ajoutera « Terminé » et « Annulé ».</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MatchStatus>))]
public enum MatchStatus
{
    Planned,
}

/// <summary>Rencontre planifiée (US-006).</summary>
public sealed class Match : Entity
{
    public const int MinPlayersPerSide = 1;
    public const int MaxPlayersPerSide = 30;
    public const int MinDurationMinutes = 10;
    public const int MaxDurationMinutes = 300;
    public const int DefaultDurationMinutes = 60;
    public const int MaxLocationLength = 100;
    public const int MaxNotesLength = 500;
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

    [JsonIgnore]
    public DateTimeOffset EndsAt => StartsAt.AddMinutes(DurationMinutes);

    /// <summary>Un match en cours n'est pas encore passé.</summary>
    public bool IsOver(DateTimeOffset now) => EndsAt <= now;
}
