#!/usr/bin/env bash
# ── ULTRON PREVIEW · manifesto do projeto ──
# servicos: lancamentos-api(8080) consolidado-api(8081) background-server(8082) web(8090)
#           postgres kafka elasticsearch(9200) kibana(5601) prometheus(9090) grafana(3000)
# compose: compose.yaml · solução: Cashflow.slnx · terminal: src/Cashflow.Terminal
# ── descreve o que este script espera encontrar; mantenha-o verdadeiro ──
#
# ./start.sh                  → up (padrão): pré-requisitos, suíte, stack, saúde, ELK, painel
# ./start.sh up [--sem-testes] → sobe a stack; --sem-testes pula a suíte
# ./start.sh test             → roda a suíte e mostra o resumo
# ./start.sh demo             → o roteiro narrado no terminal (RF3)
# ./start.sh terminal         → a UI de terminal (RF4/RF5)
# ./start.sh status           → o estado da stack e a saúde de cada serviço
# ./start.sh logs [serviço]   → os logs do compose (segue)
# ./start.sh down [--volumes] → derruba a stack; volumes só com a flag explícita
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"

VERDE=$'\e[32m'; VERMELHO=$'\e[31m'; AMARELO=$'\e[33m'; NEGRITO=$'\e[1m'; ZERO=$'\e[0m'
ok()     { printf '%s✔%s %s\n' "$VERDE" "$ZERO" "$*"; }
falha()  { printf '%s✘%s %s\n' "$VERMELHO" "$ZERO" "$*"; }
aviso()  { printf '%s!%s %s\n' "$AMARELO" "$ZERO" "$*"; }
titulo() { printf '\n%s%s%s\n' "$NEGRITO" "$*" "$ZERO"; }

# nome da rede/serviço → URL de saúde vista do host
URLS_SAUDE=(
  "lancamentos-api|http://localhost:8080/health"
  "consolidado-api|http://localhost:8081/health"
  "background-server|http://localhost:8082/health"
  "web|http://localhost:8090/health"
  "prometheus|http://localhost:9090/-/healthy"
  "elasticsearch|http://localhost:9200/_cluster/health"
  "kibana|http://localhost:5601/api/status"
  "grafana|http://localhost:3000/api/health"
)
PORTAS=(8080 8081 8082 8090 3000 5601 9090 9200)

stack_no_ar() {
  [[ -n "$(docker compose ps --status running -q 2>/dev/null | head -n1)" ]]
}

porta_ocupada() {
  (exec 3<>"/dev/tcp/127.0.0.1/$1") 2>/dev/null && exec 3>&- && return 0 || return 1
}

pre_requisitos() {
  titulo "Pré-requisitos"
  local faltou=0
  for comando in docker dotnet curl; do
    if command -v "$comando" >/dev/null 2>&1; then ok "$comando presente"; else falha "$comando não encontrado"; faltou=1; fi
  done
  if docker compose version >/dev/null 2>&1; then ok "docker compose presente"; else falha "plugin docker compose não encontrado"; faltou=1; fi
  if docker info >/dev/null 2>&1; then ok "daemon do Docker respondendo"; else falha "daemon do Docker parado"; faltou=1; fi
  if [[ "$faltou" == "1" ]]; then falha "instale o que falta e rode de novo"; exit 1; fi

  if stack_no_ar; then
    aviso "a stack já está no ar — o compose é idempotente, sigo para a atualização"
    return 0
  fi
  local ocupadas=()
  for porta in "${PORTAS[@]}"; do porta_ocupada "$porta" && ocupadas+=("$porta"); done
  if ((${#ocupadas[@]})); then
    falha "porta(s) já em uso: ${ocupadas[*]} — libere-as ou rode ./start.sh down"
    exit 1
  fi
  ok "portas ${PORTAS[*]} livres"
}

roda_suite() {
  titulo "Suíte"
  local saida
  if saida=$(dotnet test 2>&1); then
    printf '%s\n' "$saida" | tail -n 12
    ok "suíte verde"
  else
    printf '%s\n' "$saida" | tail -n 30
    falha "suíte vermelha — corrija antes de subir"
    exit 1
  fi
}

sobe_stack() {
  titulo "Stack"
  docker compose up -d --build
  ok "containers criados"
}

espera_saude() {
  titulo "Saúde"
  local prazo=240 inicio=$SECONDS
  for entrada in "${URLS_SAUDE[@]}"; do
    local nome="${entrada%%|*}" url="${entrada#*|}"
    local no_ar=0
    while ((SECONDS - inicio < prazo)); do
      if curl -fsS -o /dev/null --max-time 3 "$url" 2>/dev/null; then no_ar=1; break; fi
      sleep 2
    done
    if ((no_ar)); then ok "$nome  ($url)"; else falha "$nome não respondeu em ${prazo}s — veja ./start.sh logs $nome"; exit 1; fi
  done
  ok "todos os serviços responderam"
}

aplica_elk() {
  titulo "ELK"
  bash observability/scripts/apply-elk.sh
  ok "política de ILM, template e data view aplicados"
}

painel() {
  titulo "No ar"
  cat <<'FIM'
  Frontend          http://localhost:8090   operador / Operador@123
                                            consulta / Consulta@123
                                            loja2    / Loja2@123
  Lançamentos API   http://localhost:8080
  Consolidado API   http://localhost:8081
  Background        http://localhost:8082   (outbox e consumidor)
  Kibana (logs)     http://localhost:5601
  Grafana (métricas) http://localhost:3000  (admin/admin)
  Prometheus        http://localhost:9090
  Elasticsearch     http://localhost:9200

  Próximos comandos:
    ./start.sh demo      o roteiro narrado (lançar → consolidado → estorno → idempotência)
    ./start.sh terminal  a UI de terminal (menu por perfil)
    ./start.sh status    o estado e a saúde de cada serviço
    ./start.sh logs web  os logs de um serviço (sem argumento: todos)
    ./start.sh down      derruba a stack (volumes só com --volumes)
FIM
}

comando_up() {
  local sem_testes=0
  for arg in "$@"; do
    case "$arg" in
      --sem-testes) sem_testes=1 ;;
      *) falha "opção desconhecida para up: $arg"; exit 2 ;;
    esac
  done
  pre_requisitos
  ((sem_testes)) || roda_suite
  sobe_stack
  espera_saude
  aplica_elk
  painel
}

comando_status() {
  titulo "Containers"
  docker compose ps
  if ! stack_no_ar; then
    local respondendo=0
    for entrada in "${URLS_SAUDE[@]}"; do
      if curl -fsS -o /dev/null --max-time 2 "${entrada#*|}" 2>/dev/null; then respondendo=1; break; fi
    done
    if ((respondendo)); then
      aviso "nenhum container deste projeto compose (cashflow), mas há stack respondendo nas portas — de outro projeto? O ./start.sh up recusará enquanto elas estiverem ocupadas"
    fi
  fi
  titulo "Saúde"
  local fora=0
  for entrada in "${URLS_SAUDE[@]}"; do
    local nome="${entrada%%|*}" url="${entrada#*|}"
    if curl -fsS -o /dev/null --max-time 3 "$url" 2>/dev/null; then ok "$nome"; else falha "$nome  ($url)"; fora=1; fi
  done
  ((fora)) && return 1 || return 0
}

comando_demo() {
  if ! stack_no_ar; then falha "a stack não está no ar — rode ./start.sh (ou ./start.sh up) primeiro"; exit 1; fi
  dotnet run --project src/Cashflow.Terminal -- demo
}

comando_terminal() {
  if ! stack_no_ar; then falha "a stack não está no ar — rode ./start.sh (ou ./start.sh up) primeiro"; exit 1; fi
  dotnet run --project src/Cashflow.Terminal
}

comando_down() {
  local volumes=0
  for arg in "$@"; do
    case "$arg" in
      --volumes) volumes=1 ;;
      *) falha "opção desconhecida para down: $arg"; exit 2 ;;
    esac
  done
  titulo "Derrubando"
  if ((volumes)); then
    aviso "com --volumes os dados do PostgreSQL e do Kafka são apagados"
    docker compose down --remove-orphans --volumes
  else
    docker compose down --remove-orphans
  fi
  ok "stack derrubada"
}

case "${1:-up}" in
  up)       if (($#)); then shift; fi; comando_up "$@" ;;
  test)     roda_suite ;;
  demo)     comando_demo ;;
  terminal) comando_terminal ;;
  status)   comando_status ;;
  logs)     shift; docker compose logs -f --tail=200 "$@" ;;
  down)     shift; comando_down "$@" ;;
  -h|--help|help)
    sed -n '2,17p' "$0" | sed 's/^# \{0,1\}//'
    ;;
  *)
    falha "comando desconhecido: $1"
    sed -n '2,17p' "$0" | sed 's/^# \{0,1\}//'
    exit 2
    ;;
esac
