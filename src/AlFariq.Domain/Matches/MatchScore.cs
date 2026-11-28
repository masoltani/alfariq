namespace AlFariq.Domain;

/// <summary>Le score d'un match terminé : un entier positif ou nul par côté (RG-RES-01).</summary>
public sealed record MatchScore(int A, int B)
{
    /// <summary>L'équipe gagnante ; null pour un match nul (RG-RES-04).</summary>
    public TeamSide? Winner => A > B ? TeamSide.A : B > A ? TeamSide.B : null;

    public int For(TeamSide side) => side == TeamSide.A ? A : B;

    /// <summary>
    /// Vrai si des buts sont attribués aux joueurs d'une équipe et que leur total diffère de son score : à signaler
    /// sans bloquer (RG-RES-03). Sans aucun buteur saisi, il n'y a rien à comparer.
    /// </summary>
    public bool DiffersFrom(TeamSide side, int playerGoals) => playerGoals > 0 && playerGoals != For(side);
}
