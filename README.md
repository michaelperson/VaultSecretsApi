# VaultSecretsApi — secrets d'une API .NET 10 avec HashiCorp Vault

API ASP.NET Core **.NET 10** dont tous les secrets (clé d'API, chaîne de connexion) proviennent de **HashiCorp Vault**, lancé dans un **container Docker** pour faciliter les démonstrations.

> Support du workshop *« Gérer les secrets d'une API avec un Vault »* (bstorm training).

## Ce que démontre ce projet

- Un **provider de configuration** custom (`VaultConfigurationProvider`) qui charge `secret/myapi` dans `IConfiguration`.
- Le binding **fortement typé** (`AppSecrets`) + **rechargement** (`IOptionsSnapshot`) pour refléter une rotation sans redémarrage.
- Deux modes d'authentification : **Token** (dev) et **AppRole** (prod), choisis par configuration.
- Un **health check** qui vérifie que Vault est joignable et descellé.
- Des endpoints qui **prouvent** le chargement des secrets **sans jamais les divulguer**.

## Prérequis

- **.NET 10 SDK** (`dotnet --version` → `10.0.x`)
- **Docker** + Docker Compose

## Démarrage rapide

```bash
# 1. Lancer Vault (mode dev) + bootstrap (écrit le secret, configure AppRole)
docker compose up -d
docker compose logs bootstrap     # affiche RoleID / SecretID pour le mode prod

# 2. Lancer l'API (utilise le Token dev par défaut)
dotnet run --project src/VaultSecretsApi
```

Puis :

```bash
curl http://localhost:5080/health     # Healthy si Vault est joignable & descellé
curl http://localhost:5080/config     # { apiKeyLoaded:true, dbConfigured:true, source:"vault:secret/myapi" }
curl http://localhost:5080/api/charge # { status:"ok", keyFingerprint:"sk-d…" }  (la clé n'est jamais renvoyée)
```

## Démo : rotation à chaud

```bash
# modifier le secret dans Vault
docker exec vault-dev vault kv put secret/myapi \
  "ConnectionStrings:Default=Server=db;Database=app;User Id=api;Password=NEW;TrustServerCertificate=true" \
  "ApiKey=sk-demo-ROTATED"

# attendre Vault:ReloadSeconds (30s) puis :
curl http://localhost:5080/api/charge   # keyFingerprint reflète la nouvelle clé, sans redémarrage
```

## Passer en mode AppRole (production)

1. Récupérer `RoleId` / `SecretId` depuis `docker compose logs bootstrap`.
2. Les fournir par variables d'environnement (jamais en dur) :

```bash
export Vault__Token=            # vider le token dev
export Vault__RoleId=<role-id>
export Vault__SecretId=<secret-id>   # injecté par CI/K8s en vrai
dotnet run --project src/VaultSecretsApi
```

Le code bascule automatiquement sur AppRole dès qu'un `RoleId` est présent.

## Structure

```
VaultSecretsApi/
├─ docker-compose.yml            # Vault (dev) + bootstrap
├─ bootstrap/bootstrap.sh        # secret + policy read-only + AppRole
└─ src/VaultSecretsApi/
   ├─ Program.cs                 # wiring : AddVault → Options → HealthCheck → endpoints
   ├─ appsettings.json           # section Vault (Token dev ; RoleId/SecretId vides)
   ├─ Options/AppSecrets.cs      # options fortement typées
   └─ Vault/
      ├─ VaultOptions.cs                 # adresse, mount, path, auth, reload
      ├─ VaultClientFactory.cs           # Token OU AppRole
      ├─ VaultConfigurationProvider.cs   # lit KV v2 → IConfiguration (+ reload)
      ├─ VaultConfigurationExtensions.cs # AddVault(...) + IConfigurationSource
      └─ VaultHealthCheck.cs             # état sealed/standby de Vault
```

## Notes de sécurité

- `root-dev-token` et le mode `-dev` de Vault sont réservés à la **démo locale**. En prod : Vault scellé/HA, TLS, AppRole, audit activé.
- Les endpoints n'exposent **jamais** la valeur d'un secret (booléens / empreinte tronquée uniquement).
- Mettre la source Vault **en dernier** dans la configuration pour qu'elle surcharge `appsettings.json`.
- `appsettings.json` ne contient **aucun** secret applicatif : ils vivent tous dans Vault.
