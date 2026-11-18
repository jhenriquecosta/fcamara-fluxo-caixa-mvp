#!/usr/bin/env bash
# Aplica a política de ILM, o template de índice e a data view do Kibana.
# Idempotente: pode rodar quantas vezes for preciso. Requer que o compose esteja no ar.
set -euo pipefail

ES="${ES_URL:-http://localhost:9200}"
KIBANA="${KIBANA_URL:-http://localhost:5601}"
DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "→ ILM cashflow-logs-7d"
curl -fsS -X PUT "$ES/_ilm/policy/cashflow-logs-7d" \
  -H 'Content-Type: application/json' \
  --data-binary @"$DIR/elasticsearch/ilm-policies/cashflow-logs-7d.json" >/dev/null

echo "→ Template cashflow-logs"
curl -fsS -X PUT "$ES/_index_template/cashflow-logs" \
  -H 'Content-Type: application/json' \
  --data-binary @"$DIR/elasticsearch/index-templates/cashflow-logs.json" >/dev/null

echo "→ Data view no Kibana"
curl -fsS -X POST "$KIBANA/api/data_views/data_view" \
  -H 'kbn-xsrf: true' \
  -H 'Content-Type: application/json' \
  -d '{"data_view":{"title":"cashflow-logs-*","name":"Logs — fluxo de caixa","timeFieldName":"@timestamp"}}' >/dev/null \
  || echo "  (data view já existe ou Kibana ainda não subiu)"

echo "ELK aplicado. Kibana: $KIBANA · Elasticsearch: $ES"
