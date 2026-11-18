-- Esquema do serviço de Lançamentos.
-- Aplicado na subida pelo Migrator e pelo teste de integração (CREATE IF NOT EXISTS, idempotente).

CREATE TABLE IF NOT EXISTS lancamento (
    id              uuid PRIMARY KEY,
    comerciante_id  uuid NOT NULL,
    tipo            text NOT NULL CHECK (tipo IN ('debito', 'credito')),
    valor           numeric(18, 2) NOT NULL CHECK (valor > 0),
    descricao       text NULL,
    data            date NOT NULL,
    criado_em       timestamptz NOT NULL,
    estorno_de      uuid NULL REFERENCES lancamento (id)
);

CREATE INDEX IF NOT EXISTS ix_lancamento_comerciante_data
    ON lancamento (comerciante_id, data);

CREATE UNIQUE INDEX IF NOT EXISTS ux_lancamento_estorno_de
    ON lancamento (estorno_de) WHERE estorno_de IS NOT NULL;

CREATE TABLE IF NOT EXISTS idempotencia (
    comerciante_id     uuid NOT NULL,
    chave              text NOT NULL,
    corpo_hash         text NOT NULL,
    status             text NOT NULL CHECK (status IN ('processando', 'concluido', 'falhou')),
    lancamento_id      uuid NULL REFERENCES lancamento (id),
    resposta           jsonb NULL,
    processando_desde  timestamptz NOT NULL,
    PRIMARY KEY (comerciante_id, chave)
);

CREATE TABLE IF NOT EXISTS outbox_event (
    id           uuid PRIMARY KEY,
    event_id     uuid NOT NULL UNIQUE,
    payload      jsonb NOT NULL,
    criado_em    timestamptz NOT NULL,
    publicado_em timestamptz NULL
);

CREATE INDEX IF NOT EXISTS ix_outbox_event_pendentes
    ON outbox_event (criado_em) WHERE publicado_em IS NULL;
