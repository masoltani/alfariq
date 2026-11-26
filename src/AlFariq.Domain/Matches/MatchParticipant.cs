namespace AlFariq.Domain;

/// <summary>
/// Un joueur inscrit à un match. Entité séparée pour limiter les conflits de synchronisation.
/// Retirer un joueur d'un match supprime logiquement son inscription ; le remettre en crée une nouvelle.
/// </summary>
public sealed class MatchParticipant : Entity
{
    public Guid MatchId { get; set; }

    public Guid PlayerId { get; set; }
}
