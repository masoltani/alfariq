namespace AlFariq.Domain;

/// <summary>
/// Un joueur inscrit à un match. Entité séparée pour limiter les conflits de synchronisation.
/// Retirer un joueur d'un match supprime logiquement son inscription ; le remettre en crée une nouvelle.
/// </summary>
public sealed class MatchParticipant : Entity
{
    public Guid MatchId { get; set; }

    public Guid PlayerId { get; set; }

    /// <summary>Équipe du joueur ; null tant qu'il n'est pas assigné. Un seul champ : jamais deux équipes.</summary>
    public TeamSide? Side { get; set; }
}
