#!/bin/sh
# =============================================================================
#  Bootstrap Vault pour le workshop :
#   1. écrit le secret applicatif (KV v2)
#   2. crée une policy en lecture seule (moindre privilège)
#   3. configure AppRole et affiche RoleID / SecretID (voie production)
#  Exécuté automatiquement par le service "bootstrap" de docker-compose.
# =============================================================================
set -e

echo "[bootstrap] Attente de Vault sur $VAULT_ADDR ..."
until vault status >/dev/null 2>&1; do sleep 1; done
echo "[bootstrap] Vault prêt."

# --- 1. Secret applicatif (KV v2 est monté sur secret/ en mode dev) ----------
vault kv put secret/myapi \
  "ConnectionStrings:Default=Server=db;Database=app;User Id=api;Password=P@ssw0rd!;TrustServerCertificate=true" \
  "ApiKey=sk-demo-7f3a9c2e"
echo "[bootstrap] Secret secret/myapi écrit."

# --- 2. Policy en lecture seule sur CE chemin uniquement ---------------------
vault policy write myapi - <<'EOF'
path "secret/data/myapi" {
  capabilities = ["read"]
}
EOF
echo "[bootstrap] Policy 'myapi' (read-only) créée."

# --- 3. AppRole (production) --------------------------------------------------
vault auth enable approle 2>/dev/null || true
vault write auth/approle/role/myapi \
  token_policies="myapi" \
  token_ttl=1h \
  token_max_ttl=4h \
  secret_id_ttl=24h
ROLE_ID=$(vault read -field=role_id auth/approle/role/myapi/role-id)
SECRET_ID=$(vault write -f -field=secret_id auth/approle/role/myapi/secret-id)

echo "============================================================"
echo " AppRole prêt (voie production) :"
echo "   Vault__RoleId   = $ROLE_ID"
echo "   Vault__SecretId = $SECRET_ID"
echo " (en dev, l'API utilise simplement le Token root-dev-token)"
echo "============================================================"
echo "[bootstrap] Terminé."
