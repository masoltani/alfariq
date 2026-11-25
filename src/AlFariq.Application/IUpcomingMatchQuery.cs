namespace AlFariq.Application;

/// <summary>Ce que l'accueil affiche du prochain match.</summary>
/// <param name="Id">Identifiant du match.</param>
/// <param name="StartsAt">Date et heure de début, avec leur décalage UTC.</param>
/// <param name="Location">Lieu en texte libre ; null ou vide s'il n'est pas renseigné.</param>
/// <param name="PlayersPerSide">Format du match : nombre de joueurs par équipe.</param>
public sealed record UpcomingMatch(Guid Id, DateTimeOffset StartsAt, string? Location, int PlayersPerSide);

/// <summary>Lecture du prochain match pour l'écran d'accueil (US-003).</summary>
public interface IUpcomingMatchQuery
{
    /// <summary>Le prochain match planifié à venir, ou null s'il n'y en a pas.</summary>
    Task<UpcomingMatch?> GetNextAsync(CancellationToken cancellationToken = default);
}
