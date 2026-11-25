namespace AlFariq.Application;

public enum StoragePersistenceStatus
{
    /// <summary>Le navigateur ne propose pas le stockage persistant.</summary>
    Unsupported,

    /// <summary>Le navigateur a refusé (ou n'a pas encore accordé) la persistance : les données peuvent être effacées.</summary>
    NotPersisted,

    /// <summary>Les données locales ne seront pas effacées sans action de l'utilisateur.</summary>
    Persisted,
}

/// <summary>Demande au navigateur de ne pas effacer les données locales en cas de manque d'espace.</summary>
public interface IStoragePersistence
{
    /// <summary>Demande la persistance si elle n'est pas déjà acquise. Ne lève jamais d'exception.</summary>
    Task<StoragePersistenceStatus> RequestAsync();
}
