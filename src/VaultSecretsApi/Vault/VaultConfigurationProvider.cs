using VaultSharp.V1.Commons;

namespace VaultSecretsApi.Vault;

/// <summary>
/// Provider de configuration qui charge un secret KV v2 de Vault dans IConfiguration.
/// Chaque clé du secret (ex. "ApiKey", "ConnectionStrings:Default") devient une
/// clé de configuration standard, consommable via IConfiguration / IOptions.
/// </summary>
public sealed class VaultConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly VaultOptions _options;
    private Timer? _reloadTimer;

    public VaultConfigurationProvider(VaultOptions options) => _options = options;

    public override void Load()
    {
        LoadSecrets();

        // Rechargement périodique optionnel (reflète une rotation sans redémarrage)
        if (_options.ReloadSeconds > 0 && _reloadTimer is null)
        {
            var period = TimeSpan.FromSeconds(_options.ReloadSeconds);
            _reloadTimer = new Timer(_ => SafeReload(), null, period, period);
        }
    }

    private void LoadSecrets()
    {
        try
        {
            var client = VaultClientFactory.Create(_options);

            // ConfigurationProvider.Load() est synchrone : on attend l'appel async.
            Secret<SecretData> secret = client.V1.Secrets.KeyValue.V2
                .ReadSecretAsync(path: _options.Path, mountPoint: _options.MountPoint)
                .GetAwaiter().GetResult();

            Data = secret.Data.Data.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.ToString(),
                StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (_options.Optional)
        {
            // Mode tolérant : on démarre sans les secrets (ils seront simplement absents).
            Console.Error.WriteLine($"[Vault] Chargement optionnel échoué : {ex.Message}");
            Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void SafeReload()
    {
        try
        {
            LoadSecrets();
            OnReload(); // notifie IConfiguration -> IOptionsSnapshot / IOptionsMonitor
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Vault] Rechargement échoué : {ex.Message}");
        }
    }

    public void Dispose() => _reloadTimer?.Dispose();
}
