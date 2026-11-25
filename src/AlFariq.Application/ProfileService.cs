using AlFariq.Domain;

namespace AlFariq.Application;

/// <summary>Saisie du profil. Seul le pseudo est obligatoire.</summary>
public sealed record ProfileInput(
    string Nickname,
    string SportCode = SportCatalog.FootballCode,
    string? PreferredPositionCode = null);

/// <summary>
/// Création et modification du profil local (US-004). La personne du profil figure aussi dans le répertoire
/// de joueurs : le joueur correspondant est créé puis tenu à jour à chaque enregistrement.
/// </summary>
public sealed class ProfileService
{
    private readonly IRepository<Profile> _profiles;
    private readonly IRepository<Player> _players;

    // Un enregistrement à la fois : deux envois rapprochés ne doivent pas créer deux profils.
    private readonly SemaphoreSlim _saveGate = new(1, 1);

    public ProfileService(IRepository<Profile> profiles, IRepository<Player> players)
    {
        _profiles = profiles;
        _players = players;
    }

    /// <summary>Le profil local, ou null s'il n'a pas encore été créé.</summary>
    public async Task<Profile?> GetAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await _profiles.ListAsync(cancellationToken: cancellationToken);
        return profiles.Count > 0 ? profiles[0] : null;
    }

    /// <summary>Crée le profil ou le modifie. Peut être relancé sans créer de doublon.</summary>
    /// <exception cref="ArgumentException">Pseudo vide ou trop long, sport inconnu, poste d'un autre sport.</exception>
    public async Task<Profile> SaveAsync(ProfileInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var nickname = PlayerDirectory.CleanName(input.Nickname);
        if (nickname.Length == 0 || nickname.Length > Profile.MaxNicknameLength)
        {
            throw new ArgumentException(
                $"Le pseudo doit contenir de 1 à {Profile.MaxNicknameLength} caractères.", nameof(input));
        }

        var sport = SportCatalog.Find(input.SportCode)
            ?? throw new ArgumentException($"Sport inconnu : « {input.SportCode} ».", nameof(input));

        if (input.PreferredPositionCode is not null && !sport.HasPosition(input.PreferredPositionCode))
        {
            throw new ArgumentException(
                $"Le poste « {input.PreferredPositionCode} » n'existe pas dans ce sport.", nameof(input));
        }

        await _saveGate.WaitAsync(cancellationToken);
        try
        {
            var existing = await GetAsync(cancellationToken);
            var profile = existing ?? new Profile();
            profile.Nickname = nickname;
            profile.SportCode = sport.Code;
            profile.PreferredPositionCode = input.PreferredPositionCode;

            profile = existing is null
                ? await _profiles.AddAsync(profile, cancellationToken)
                : await _profiles.UpdateAsync(profile, cancellationToken);

            await SaveOwnPlayerAsync(profile, cancellationToken);
            return profile;
        }
        finally
        {
            _saveGate.Release();
        }
    }

    // Les deux écritures ne sont pas atomiques : si celle-ci échoue, l'enregistrement suivant la rattrape.
    private async Task SaveOwnPlayerAsync(Profile profile, CancellationToken cancellationToken)
    {
        var players = await _players.ListAsync(cancellationToken: cancellationToken);
        var existing = players.FirstOrDefault(player => player.ProfileId == profile.Id);
        var own = existing ?? new Player { ProfileId = profile.Id };
        own.Name = profile.Nickname;
        own.PreferredPositionCode = profile.PreferredPositionCode;

        if (existing is null)
        {
            await _players.AddAsync(own, cancellationToken);
        }
        else
        {
            await _players.UpdateAsync(own, cancellationToken);
        }
    }
}
