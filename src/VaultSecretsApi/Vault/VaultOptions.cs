namespace VaultSecretsApi.Vault;

/// <summary>
/// Options de connexion à Vault, liées à la section "Vault" de la configuration.
/// </summary>
public sealed class VaultOptions
{
    /// <summary>URL du serveur Vault (HTTPS en production).</summary>
    public string Address { get; set; } = "http://localhost:8200";

    /// <summary>Point de montage du moteur KV v2 (par défaut "secret" en mode dev).</summary>
    public string MountPoint { get; set; } = "secret";

    /// <summary>Chemin logique du secret applicatif.</summary>
    public string Path { get; set; } = "myapi";

    // --- Authentification : Token (dev) OU AppRole (prod) ---

    /// <summary>Token Vault (mode dev). Ignoré si un RoleId est fourni.</summary>
    public string? Token { get; set; }

    /// <summary>RoleID AppRole (stable, peu sensible) — voie production.</summary>
    public string? RoleId { get; set; }

    /// <summary>SecretID AppRole (sensible, court) — injecté à l'exécution.</summary>
    public string? SecretId { get; set; }

    /// <summary>
    /// Intervalle de rechargement périodique des secrets, en secondes.
    /// 0 = pas de rechargement automatique.
    /// </summary>
    public int ReloadSeconds { get; set; } = 0;

    /// <summary>
    /// Si true, l'application démarre même si Vault est injoignable
    /// (les secrets seront simplement absents). À false : on échoue vite (fail-fast).
    /// </summary>
    public bool Optional { get; set; } = false;

    public bool UsesAppRole => !string.IsNullOrWhiteSpace(RoleId);
}
