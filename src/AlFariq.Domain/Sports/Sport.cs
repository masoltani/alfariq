namespace AlFariq.Domain;

/// <summary>Un sport du catalogue : son code et les postes qu'on peut y préférer.</summary>
/// <param name="Code">Identifiant stable, enregistré avec les données (ne jamais le renommer).</param>
/// <param name="PositionCodes">Codes de postes, préfixés par le sport (« football.goalkeeper »).</param>
public sealed record Sport(string Code, IReadOnlyList<string> PositionCodes)
{
    public bool HasPosition(string? positionCode) => positionCode is not null && PositionCodes.Contains(positionCode);
}
