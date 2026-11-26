using AlFariq.Domain;

namespace AlFariq.UI.Components;

/// <summary>Un bouton « équipe » d'une ligne de joueur.</summary>
public sealed record TeamButton(TeamSide Side, string Label, string ColorCode);
