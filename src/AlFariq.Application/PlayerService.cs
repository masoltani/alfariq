using AlFariq.Domain;

namespace AlFariq.Application;

/// <summary>Saisie d'un joueur. Seul le nom est obligatoire.</summary>
public sealed record PlayerInput(string Name, string? PreferredPositionCode = null, int? Level = null);

/// <summary>
/// Répertoire de joueurs (US-005). Les doublons de nom sont permis : l'interface se contente d'avertir
/// (<see cref="PlayerDirectory.HasSameName"/>).
/// </summary>
/// <remarks>
/// Le joueur lié au profil (« moi ») tient son nom et son poste du profil : seul son niveau se modifie ici,
/// et il ne s'archive pas.
/// </remarks>
public sealed class PlayerService
{
    private readonly IRepository<Player> _players;

    public PlayerService(IRepository<Player> players) => _players = players;

    /// <summary>Tous les joueurs du répertoire, archivés compris.</summary>
    public Task<IReadOnlyList<Player>> ListAsync(CancellationToken cancellationToken = default) =>
        _players.ListAsync(cancellationToken: cancellationToken);

    /// <summary>Les joueurs à proposer dans une sélection : non archivés, triés par nom.</summary>
    public async Task<IReadOnlyList<Player>> ListSelectableAsync(CancellationToken cancellationToken = default)
    {
        var players = await ListAsync(cancellationToken);
        return PlayerDirectory.Search(players.Where(player => !player.IsArchived), query: null);
    }

    /// <summary>
    /// Un joueur par son identifiant, même archivé. Pour l'historique d'un match, <paramref name="includeDeleted"/>
    /// retrouve aussi un joueur supprimé.
    /// </summary>
    public Task<Player?> FindAsync(Guid id, bool includeDeleted = false, CancellationToken cancellationToken = default) =>
        _players.GetAsync(id, includeDeleted, cancellationToken);

    /// <exception cref="ArgumentException">Nom vide ou trop long, poste inconnu, niveau hors de 1 à 5.</exception>
    public async Task<Player> AddAsync(PlayerInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var player = new Player();
        ApplyIdentity(player, input);
        ApplyLevel(player, input.Level);
        return await _players.AddAsync(player, cancellationToken);
    }

    /// <exception cref="ArgumentException">Nom vide ou trop long, poste inconnu, niveau hors de 1 à 5.</exception>
    /// <exception cref="KeyNotFoundException">Le joueur n'existe pas ou est supprimé.</exception>
    public async Task<Player> UpdateAsync(Guid id, PlayerInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var player = await RequireAsync(id, cancellationToken);
        if (player.ProfileId is null)
        {
            ApplyIdentity(player, input);
        }

        ApplyLevel(player, input.Level);
        return await _players.UpdateAsync(player, cancellationToken);
    }

    /// <summary>Archive un joueur ou le réactive.</summary>
    /// <exception cref="InvalidOperationException">On tente d'archiver le joueur lié au profil.</exception>
    /// <exception cref="KeyNotFoundException">Le joueur n'existe pas ou est supprimé.</exception>
    public async Task<Player> SetArchivedAsync(Guid id, bool archived, CancellationToken cancellationToken = default)
    {
        var player = await RequireAsync(id, cancellationToken);
        if (archived && player.ProfileId is not null)
        {
            throw new InvalidOperationException("Le joueur lié au profil ne peut pas être archivé.");
        }

        player.IsArchived = archived;
        return await _players.UpdateAsync(player, cancellationToken);
    }

    private async Task<Player> RequireAsync(Guid id, CancellationToken cancellationToken) =>
        await _players.GetAsync(id, cancellationToken: cancellationToken)
        ?? throw new KeyNotFoundException($"Aucun joueur actif avec l'identifiant {id}.");

    private static void ApplyIdentity(Player player, PlayerInput input)
    {
        var name = PlayerDirectory.CleanName(input.Name);
        if (name.Length == 0 || name.Length > Player.MaxNameLength)
        {
            throw new ArgumentException(
                $"Le nom doit contenir de 1 à {Player.MaxNameLength} caractères.", nameof(input));
        }

        if (input.PreferredPositionCode is not null && !SportCatalog.IsKnownPosition(input.PreferredPositionCode))
        {
            throw new ArgumentException($"Poste inconnu : « {input.PreferredPositionCode} ».", nameof(input));
        }

        player.Name = name;
        player.PreferredPositionCode = input.PreferredPositionCode;
    }

    private static void ApplyLevel(Player player, int? level)
    {
        if (level is < Player.MinLevel or > Player.MaxLevel)
        {
            throw new ArgumentException(
                $"Le niveau doit être compris entre {Player.MinLevel} et {Player.MaxLevel}.", nameof(level));
        }

        player.Level = level;
    }
}
