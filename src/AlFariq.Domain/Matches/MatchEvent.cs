using System.Text.Json.Serialization;

namespace AlFariq.Domain;

/// <summary>Nature d'un événement de match, stockée en texte.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MatchEventType>))]
public enum MatchEventType
{
    Goal,
    Assist,
}

/// <summary>
/// Un but ou une passe décisive (US-012, RG-RES-02), facultatif. Le participant est désigné par le couple match et
/// joueur : ses buts le suivent s'il change d'équipe, ou s'il est retiré du match puis réinscrit.
/// </summary>
public sealed class MatchEvent : Entity
{
    public Guid MatchId { get; set; }

    public Guid PlayerId { get; set; }

    public MatchEventType Type { get; set; }

    /// <summary>Minute de jeu, si elle est connue ; pas encore saisie.</summary>
    public int? Minute { get; set; }
}
