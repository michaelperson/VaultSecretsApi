using Microsoft.Extensions.Diagnostics.HealthChecks;
using VaultSharp;

namespace VaultSecretsApi.Vault;

/// <summary>
/// Vérifie que Vault est joignable et descellé. Un Vault scellé ne peut servir
/// aucun secret : on le signale aux sondes (readiness Kubernetes, load balancer).
/// </summary>
public sealed class VaultHealthCheck : IHealthCheck
{
    private readonly IVaultClient _client;
    public VaultHealthCheck(IVaultClient client) => _client = client;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var health = await _client.V1.System.GetHealthStatusAsync();
            if (!health.Initialized)
                return HealthCheckResult.Unhealthy("Vault non initialisé.");
            if (health.Sealed)
                return HealthCheckResult.Unhealthy("Vault scellé (sealed).");

            return HealthCheckResult.Healthy(
                health.Standby ? "Vault OK (nœud standby)." : "Vault OK (actif).");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Vault injoignable.", ex);
        }
    }
}
