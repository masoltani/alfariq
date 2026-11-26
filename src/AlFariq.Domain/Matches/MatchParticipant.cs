namespace AlFariq.Domain;

/// <summary>
/// Un joueur inscrit à un match. Entité séparée pour limiter les conflits de synchronisation.
/// US-007 ajoutera la sélection ; en attendant, seule la duplication d'un match recopie ces inscriptions.
/// </summary>
public sealed class MatchParticipant : Entity
{
    public Guid MatchId { get; set; }

    public Guid PlayerId { get; set; }
}
