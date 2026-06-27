using Microsoft.Extensions.Options;
using VaultSecretsApi.Options;
using VaultSecretsApi.Vault;
using VaultSharp;

var builder = WebApplication.CreateBuilder(args);

// 1. Brancher Vault comme source de configuration (EN DERNIER : il surcharge appsettings).
//    Les clés du secret KV ("ApiKey", "ConnectionStrings:Default") deviennent
//    des clés de configuration standard.
builder.Configuration.AddVault(builder.Configuration.GetSection("Vault"));

// 2. Options fortement typées, liées à la configuration (donc rechargeables).
builder.Services.Configure<AppSecrets>(builder.Configuration);

// 3. Client Vault partagé (singleton) pour le health check.
var vaultOptions = builder.Configuration.GetSection("Vault").Get<VaultOptions>()!;
builder.Services.AddSingleton<IVaultClient>(_ => VaultClientFactory.Create(vaultOptions));

// 4. Health check Vault.
builder.Services.AddHealthChecks()
    .AddCheck<VaultHealthCheck>("vault", tags: ["ready"]);

var app = builder.Build();

app.MapHealthChecks("/health");

// /config : PROUVE que les secrets sont chargés, SANS jamais les divulguer.
app.MapGet("/config", (IOptionsSnapshot<AppSecrets> secrets, IConfiguration config) => new
{
    apiKeyLoaded = !string.IsNullOrEmpty(secrets.Value.ApiKey),
    dbConfigured = !string.IsNullOrEmpty(config.GetConnectionString("Default")),
    source = "vault:secret/myapi",
});

// Démo « usage réel » : on se sert du secret côté serveur sans l'exposer.
app.MapGet("/api/charge", (IOptionsSnapshot<AppSecrets> secrets) =>
{
    if (string.IsNullOrEmpty(secrets.Value.ApiKey))
        return Results.Problem("Secret indisponible : Vault est-il démarré et bootstrappé ?", statusCode: 503);

    // En vrai : appel au service tiers avec secrets.Value.ApiKey.
    // On renvoie uniquement une empreinte tronquée, jamais la clé.
    var fingerprint = secrets.Value.ApiKey.Length >= 4
        ? secrets.Value.ApiKey[..4] + "…"
        : "…";
    return Results.Ok(new { status = "ok", keyFingerprint = fingerprint });
});

app.Run();
