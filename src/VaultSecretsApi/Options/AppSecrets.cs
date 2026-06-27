namespace VaultSecretsApi.Options;

/// <summary>
/// Secrets applicatifs, alimentés depuis Vault via le provider de configuration.
/// Liés à la racine de IConfiguration : les clés du secret KV ("ApiKey",
/// "ConnectionStrings:Default") deviennent des clés de configuration standard.
/// </summary>
public sealed class AppSecrets
{
    /// <summary>Clé d'API d'un service tiers (ex. paiement, e-mail).</summary>
    public string ApiKey { get; init; } = string.Empty;
}
