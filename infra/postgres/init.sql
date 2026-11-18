-- O banco `cashflow` é criado pelo POSTGRES_DB, com dono `cashflow`.
CREATE DATABASE cashflow_consolidado OWNER cashflow;

-- Um usuário por serviço (ADR-0003): cada um só conecta e escreve no seu banco.
CREATE ROLE lancamentos LOGIN PASSWORD 'lancamentos';
CREATE ROLE consolidado LOGIN PASSWORD 'consolidado';

GRANT CONNECT ON DATABASE cashflow TO lancamentos;
GRANT CONNECT ON DATABASE cashflow_consolidado TO consolidado;

\connect cashflow
GRANT USAGE, CREATE ON SCHEMA public TO lancamentos;

\connect cashflow_consolidado
GRANT USAGE, CREATE ON SCHEMA public TO consolidado;
