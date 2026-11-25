namespace AlFariq.Domain;

/// <summary>Profil local de la personne qui utilise l'appareil (US-004). Il n'y en a qu'un.</summary>
public sealed class Profile : Entity
{
    public const int MaxNicknameLength = 40;

    /// <summary>Nom ou pseudo (obligatoire, pas forcément unique).</summary>
    public string Nickname { get; set; } = "";

    /// <summary>Sport par défaut (code de <see cref="SportCatalog"/>).</summary>
    public string SportCode { get; set; } = SportCatalog.FootballCode;

    /// <summary>Poste préféré dans ce sport ; null si aucune préférence.</summary>
    public string? PreferredPositionCode { get; set; }
}
