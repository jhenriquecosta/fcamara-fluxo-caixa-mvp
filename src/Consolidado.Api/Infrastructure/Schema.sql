-- Esquema do serviço de Consolidado.
-- Aplicado na subida pelo Migrator e pelo teste de integração (CREATE IF NOT EXISTS, idempotente).

CREATE TABLE IF NOT EXISTS consolidado_diario (
    comerciante_id  uuid NOT NULL,
    data            date NOT NULL,
    saldo_dia       numeric(18, 2) NOT NULL DEFAULT 0,
    saldo_acumulado numeric(18, 2) NOT NULL DEFAULT 0,
    PRIMARY KEY (comerciante_id, data)
);

CREATE TABLE IF NOT EXISTS evento_processado (
    event_id      uuid PRIMARY KEY,
    processado_em timestamptz NOT NULL
);
