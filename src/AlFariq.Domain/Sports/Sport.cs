namespace AlFariq.Domain;

/// <summary>Un sport du catalogue : son code, les postes qu'on peut y préférer et ses formats courants.</summary>
/// <param name="Code">Identifiant stable, enregistré avec les données (ne jamais le renommer).</param>
/// <param name="PositionCodes">Codes de postes, préfixés par le sport (« football.goalkeeper »).</param>
/// <param name="Formats">Nombres de joueurs par équipe proposés ; le premier est le format par défaut.</param>
public sealed record Sport(string Code, IReadOnlyList<string> PositionCodes, IReadOnlyList<int> Formats)
{
    public int DefaultPlayersPerSide => Formats[0];

    public bool HasPosition(string? positionCode) => positionCode is not null && PositionCodes.Contains(positionCode);
}
