namespace VaultSecretsApi.Vault;

/// <summary>Source de configuration : fabrique le provider Vault.</summary>
public sealed class VaultConfigurationSource : IConfigurationSource
{
    private readonly VaultOptions _options;
    public VaultConfigurationSource(VaultOptions options) => _options = options;

    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new VaultConfigurationProvider(_options);
}

/// <summary>Méthode d'extension : branche Vault sur le pipeline de configuration.</summary>
public static class VaultConfigurationExtensions
{
    public static IConfigurationBuilder AddVault(
        this IConfigurationBuilder builder, IConfiguration vaultSection)
    {
        var options = new VaultOptions();
        vaultSection.Bind(options);
        return builder.Add(new VaultConfigurationSource(options));
    }
}
