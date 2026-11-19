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

-- Migração aditiva (task 0004): o outbox passa a guardar o tipo do evento e o objeto de origem,
-- como o OutboxEvent da referência. Bancos criados antes recebem as colunas com o valor tirado
-- do envelope já gravado; rodar de novo não muda nada.
ALTER TABLE outbox_event ADD COLUMN IF NOT EXISTS event_type text;
ALTER TABLE outbox_event ADD COLUMN IF NOT EXISTS object_id text;

UPDATE outbox_event
SET event_type = payload->>'eventType',
    object_id  = payload->'payload'->>'lancamentoId'
WHERE event_type IS NULL OR object_id IS NULL;

ALTER TABLE outbox_event ALTER COLUMN event_type SET NOT NULL;
ALTER TABLE outbox_event ALTER COLUMN object_id SET NOT NULL;
