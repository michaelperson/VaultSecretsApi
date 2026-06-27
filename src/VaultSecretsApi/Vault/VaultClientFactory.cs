using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.AuthMethods.Token;

namespace VaultSecretsApi.Vault;

/// <summary>
/// Construit un IVaultClient à partir des options : AppRole si un RoleId est
/// présent (production), sinon Token (mode dev). Le reste du code ne dépend
/// jamais de la méthode d'authentification choisie.
/// </summary>
public static class VaultClientFactory
{
    public static IVaultClient Create(VaultOptions o)
    {
        IAuthMethodInfo auth = o.UsesAppRole
            ? new AppRoleAuthMethodInfo(o.RoleId, o.SecretId)
            : new TokenAuthMethodInfo(
                o.Token ?? throw new InvalidOperationException(
                    "Aucun mode d'authentification Vault : fournissez Vault:Token (dev) ou Vault:RoleId + Vault:SecretId (prod)."));

        var settings = new VaultClientSettings(o.Address, auth);
        return new VaultClient(settings);
    }
}
