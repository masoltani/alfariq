using System.Text.Json.Serialization;

namespace AlFariq.Domain;

/// <summary>
/// Base de toutes les entités stockées localement (puis synchronisées, US-022).
/// L'identifiant est créé côté client (GUID version 7, triable par date) : une entité créée hors ligne
/// a déjà son identifiant définitif. Les dates sont gérées par le dépôt, en UTC.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Date de suppression logique ; null tant que l'entité est active.</summary>
    public DateTimeOffset? DeletedAt { get; set; }

    [JsonIgnore]
    public bool IsDeleted => DeletedAt is not null;
}
