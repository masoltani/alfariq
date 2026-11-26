using System.Text.Json;
using System.Text.Json.Serialization;

namespace AlFariq.Client.Core.Storage;

/// <summary>Index d'un magasin d'objets IndexedDB (par exemple la date d'un match).</summary>
public sealed record IndexDefinition(string Name, string KeyPath, bool Unique = false);

/// <summary>Magasin d'objets IndexedDB : un par type d'entité.</summary>
public sealed record StoreDefinition(string Name, IReadOnlyList<IndexDefinition>? Indexes = null);

/// <summary>Une étape de migration : ce que la version <see cref="Version"/> de la base locale ajoute.</summary>
public sealed record SchemaMigration(int Version, IReadOnlyList<StoreDefinition> CreateStores);

/// <summary>
/// Schéma de la base locale. Le numéro de version de la base IndexedDB est celui de la dernière migration.
/// À l'ouverture, le navigateur compare sa version à <see cref="CurrentVersion"/> et le module JavaScript
/// n'applique que les migrations manquantes, dans l'ordre.
/// </summary>
/// <remarks>
/// Pour ajouter une entité (par exemple dans US-004) : ajouter une migration avec la version suivante qui crée son
/// magasin. Ne jamais modifier une migration déjà livrée : les appareils qui l'ont appliquée ne la rejouent pas.
/// </remarks>
public static class LocalSchema
{
    public const string DatabaseName = "alfariq";

    /// <summary>Magasin technique (identifiant d'appareil, réglages internes).</summary>
    public const string MetaStore = "meta";

    /// <summary>Profil local (US-004).</summary>
    public const string ProfilesStore = "profiles";

    /// <summary>Répertoire de joueurs (US-004, complété par US-005).</summary>
    public const string PlayersStore = "players";

    /// <summary>Matchs (US-006).</summary>
    public const string MatchesStore = "matches";

    /// <summary>Joueurs inscrits aux matchs (US-006, complété par US-007).</summary>
    public const string MatchParticipantsStore = "match_participants";

    /// <summary>Réglages des équipes d'un match : nom et couleur (US-008).</summary>
    public const string MatchSidesStore = "match_sides";

    public static IReadOnlyList<SchemaMigration> Migrations { get; } =
    [
        new SchemaMigration(1, [new StoreDefinition(MetaStore)]),
        new SchemaMigration(2, [new StoreDefinition(ProfilesStore), new StoreDefinition(PlayersStore)]),
        new SchemaMigration(3, [new StoreDefinition(MatchesStore), new StoreDefinition(MatchParticipantsStore)]),
        new SchemaMigration(4, [new StoreDefinition(MatchSidesStore)]),
    ];

    public static int CurrentVersion => Migrations[Migrations.Count - 1].Version;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static bool ContainsStore(string name) =>
        Migrations.SelectMany(m => m.CreateStores).Any(s => s.Name == name);

    /// <summary>Sérialise les migrations pour le module JavaScript (clés en camelCase).</summary>
    public static string ToJson(IReadOnlyList<SchemaMigration> migrations) =>
        JsonSerializer.Serialize(migrations, JsonOptions);

    /// <summary>Vérifie qu'une liste de migrations est cohérente : versions 1, 2, 3... et noms de magasins uniques.</summary>
    /// <exception cref="InvalidOperationException">La liste est incohérente.</exception>
    public static void Validate(IReadOnlyList<SchemaMigration> migrations)
    {
        ArgumentNullException.ThrowIfNull(migrations);

        if (migrations.Count == 0)
        {
            throw new InvalidOperationException("Le schéma local doit contenir au moins une migration.");
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < migrations.Count; i++)
        {
            var migration = migrations[i];
            if (migration.Version != i + 1)
            {
                throw new InvalidOperationException(
                    $"Les versions du schéma local doivent se suivre à partir de 1 : attendu {i + 1}, trouvé {migration.Version}.");
            }

            foreach (var store in migration.CreateStores)
            {
                if (string.IsNullOrWhiteSpace(store.Name))
                {
                    throw new InvalidOperationException($"La migration {migration.Version} déclare un magasin sans nom.");
                }

                if (!seen.Add(store.Name))
                {
                    throw new InvalidOperationException($"Le magasin « {store.Name} » est déclaré plusieurs fois.");
                }
            }
        }
    }
}
