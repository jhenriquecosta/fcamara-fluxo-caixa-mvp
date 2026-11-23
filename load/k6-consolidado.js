// Prova de carga do RNF-2: o consolidado recebe 50 requisições por segundo com, no máximo,
// 5% de perda. Roda contra a stack local com o token de desenvolvimento do ADR-0010, pedido ao
// próprio consolidado: a carga não depende da API de lançamentos.
//
// O `./start.sh load` roda o k6 na rede do compose, igual em Linux, macOS e Windows:
//
//   docker run --rm --network cashflow_default -e CONSOLIDADO_URL=http://consolidado-api:8080 \
//     -v "$PWD/load:/scripts" grafana/k6:2.4.0 run /scripts/k6-consolidado.js
//
// Variáveis: CONSOLIDADO_URL (padrão http://localhost:8081, para o k6 instalado no host), COMERCIANTE_ID.
import http from 'k6/http';
import { check } from 'k6';

export const options = {
    scenarios: {
        consolidado: {
            executor: 'constant-arrival-rate',
            rate: 50,
            timeUnit: '1s',
            duration: '60s',
            preAllocatedVUs: 50,
            maxVUs: 150,
        },
    },
    thresholds: {
        http_req_failed: ['rate<0.05'],
        http_req_duration: ['p(95)<200'],
    },
};

const CONSOLIDADO = __ENV.CONSOLIDADO_URL || 'http://localhost:8081';
const COMERCIANTE = __ENV.COMERCIANTE_ID || '7c9e6679-7425-40de-944b-e07fc1f90ae7';

export function setup() {
    const resposta = http.post(
        `${CONSOLIDADO}/dev/token`,
        JSON.stringify({ comercianteId: COMERCIANTE, scopes: ['consolidado.read'] }),
        { headers: { 'Content-Type': 'application/json' } });
    return {
        token: resposta.json('token'),
        // São Paulo é UTC-3 desde 2019: o dia do registro é o local, sem depender de Intl.
        dia: new Date(Date.now() - 3 * 3600 * 1000).toISOString().slice(0, 10),
    };
}

export default function (dados) {
    const resposta = http.get(
        `${CONSOLIDADO}/api/v1/consolidado/${dados.dia}`,
        { headers: { Authorization: `Bearer ${dados.token}` } });
    check(resposta, { 'resposta 200': (r) => r.status === 200 });
}
