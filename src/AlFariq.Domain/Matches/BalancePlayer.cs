namespace AlFariq.Domain;

/// <summary>Un joueur à répartir par <see cref="TeamBalancer"/>.</summary>
/// <param name="Id">Identifiant du joueur.</param>
/// <param name="Level">Niveau perçu ; null pour compter <see cref="TeamBalancer.DefaultLevel"/>.</param>
/// <param name="PositionCode">Poste à répartir entre les équipes ; null s'il n'y en a pas.</param>
/// <param name="LockedSide">Côté imposé par l'organisateur ; null si le joueur est libre.</param>
public sealed record BalancePlayer(Guid Id, int? Level, string? PositionCode = null, TeamSide? LockedSide = null)
{
    /// <summary>
    /// Un joueur du répertoire pour un match de ce sport : un poste d'un autre sport est ignoré, et une variante
    /// compte comme son poste général (un arrière gauche est un défenseur).
    /// </summary>
    public static BalancePlayer From(Player player, Sport? sport, TeamSide? lockedSide = null)
    {
        ArgumentNullException.ThrowIfNull(player);

        var position = player.PreferredPositionCode is { } code && sport?.HasPosition(code) == true
            ? SportCatalog.BaseOf(code)
            : null;
        return new BalancePlayer(player.Id, player.Level, position, lockedSide);
    }
}
