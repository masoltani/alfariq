namespace AlFariq.Domain;

/// <summary>Joueur du répertoire (US-005).</summary>
public sealed class Player : Entity
{
    public const int MaxNameLength = Profile.MaxNicknameLength;

    public const int MinLevel = 1;

    public const int MaxLevel = 5;

    /// <summary>Prénom, nom ou pseudo (obligatoire, pas forcément unique).</summary>
    public string Name { get; set; } = "";

    public string? PreferredPositionCode { get; set; }

    /// <summary>Niveau perçu par l'organisateur, de <see cref="MinLevel"/> à <see cref="MaxLevel"/> ; null s'il n'est pas renseigné.</summary>
    public int? Level { get; set; }

    /// <summary>Un joueur archivé n'est plus proposé dans les sélections mais reste dans l'historique.</summary>
    public bool IsArchived { get; set; }

    /// <summary>Renseigné quand ce joueur est la personne du profil ; null pour les autres.</summary>
    public Guid? ProfileId { get; set; }
}
