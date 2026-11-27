namespace AlFariq.Domain;

/// <summary>Un emplacement du terrain.</summary>
/// <param name="Index">Rang dans la formation, de l'arrière vers l'avant puis de gauche à droite ; enregistré avec les données.</param>
/// <param name="PositionCode">Poste de cet emplacement, code de <see cref="SportCatalog"/>.</param>
public sealed record FormationSlot(int Index, string PositionCode)
{
    /// <summary>À quel point un poste convient ici : 2 pour le même, 1 pour le même poste général, 0 sinon.</summary>
    public int Affinity(string? positionCode)
    {
        if (positionCode is null)
        {
            return 0;
        }

        if (positionCode == PositionCode)
        {
            return 2;
        }

        return SportCatalog.BaseOf(positionCode) == SportCatalog.BaseOf(PositionCode) ? 1 : 0;
    }
}

/// <summary>Un joueur d'une équipe à répartir sur les emplacements d'une formation.</summary>
/// <param name="PlayerId">Identifiant du joueur.</param>
/// <param name="PositionCode">Son poste actuel pour ce match ; null s'il n'en a pas.</param>
/// <param name="WasOnPitch">Vrai s'il occupait déjà un emplacement.</param>
public sealed record SlotCandidate(Guid PlayerId, string? PositionCode, bool WasOnPitch);

/// <summary>Une disposition d'une équipe sur le terrain, gardien compris.</summary>
/// <param name="Code">Nom usuel (« 4-4-2 »), unique pour un format et enregistré avec les données.</param>
/// <param name="Lines">Les lignes d'emplacements, de l'arrière vers l'avant, chacune de gauche à droite.</param>
public sealed record Formation(string Code, IReadOnlyList<IReadOnlyList<FormationSlot>> Lines)
{
    /// <summary>Tous les emplacements, dans l'ordre de leur rang.</summary>
    public IReadOnlyList<FormationSlot> Slots { get; } = Lines.SelectMany(line => line).ToList();

    public int PlayersPerSide => Slots.Count;

    public bool HasGoalkeeper => Slots.Any(slot => slot.PositionCode.EndsWith(".goalkeeper", StringComparison.Ordinal));

    /// <summary>
    /// Replace les joueurs quand on choisit cette formation : d'abord sur leur poste exact, puis sur le même poste
    /// général. Ceux qui étaient déjà sur le terrain passent en premier et prennent ensuite les places qui restent,
    /// sauf le but : personne ne devient gardien automatiquement. Les autres joueurs ne sont placés que si leur poste
    /// correspond.
    /// </summary>
    /// <returns>L'emplacement de chaque joueur placé.</returns>
    public IReadOnlyDictionary<Guid, int> Distribute(IReadOnlyList<SlotCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var ordered = candidates.OrderByDescending(candidate => candidate.WasOnPitch).ToList();
        var free = Slots.ToList();
        var placed = new Dictionary<Guid, int>();

        Fill((candidate, slot) => slot.Affinity(candidate.PositionCode) == 2);
        Fill((candidate, slot) => slot.Affinity(candidate.PositionCode) == 1);
        Fill((candidate, slot) =>
            candidate.WasOnPitch && !slot.PositionCode.EndsWith(".goalkeeper", StringComparison.Ordinal));

        return placed;

        void Fill(Func<SlotCandidate, FormationSlot, bool> fits)
        {
            foreach (var candidate in ordered.Where(candidate => !placed.ContainsKey(candidate.PlayerId)))
            {
                var slot = free.FirstOrDefault(open => fits(candidate, open));
                if (slot is not null)
                {
                    placed[candidate.PlayerId] = slot.Index;
                    free.Remove(slot);
                }
            }
        }
    }
}
