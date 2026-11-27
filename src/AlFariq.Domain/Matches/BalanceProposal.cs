namespace AlFariq.Domain;

/// <summary>Une répartition proposée. Ce n'est qu'une proposition : rien n'est enregistré.</summary>
/// <param name="Sides">Le côté de chaque joueur.</param>
/// <param name="LevelA">Niveau total de l'équipe A.</param>
/// <param name="LevelB">Niveau total de l'équipe B.</param>
/// <param name="GroupedPositions">Postes tenus par au moins deux joueurs, tous placés dans la même équipe.</param>
public sealed record BalanceProposal(
    IReadOnlyDictionary<Guid, TeamSide> Sides,
    int LevelA,
    int LevelB,
    IReadOnlyList<string> GroupedPositions)
{
    /// <summary>Écart de niveau total entre les deux équipes.</summary>
    public int Gap => Math.Abs(LevelA - LevelB);
}
