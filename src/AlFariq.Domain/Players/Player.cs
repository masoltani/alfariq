namespace AlFariq.Domain;

/// <summary>Joueur du répertoire. US-005 le complétera (niveau perçu, archivage).</summary>
public sealed class Player : Entity
{
    public string Name { get; set; } = "";

    public string? PreferredPositionCode { get; set; }

    /// <summary>Renseigné quand ce joueur est la personne du profil ; null pour les autres.</summary>
    public Guid? ProfileId { get; set; }
}
