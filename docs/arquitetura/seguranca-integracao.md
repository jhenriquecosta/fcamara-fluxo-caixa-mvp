# Segurança para consumo dos serviços

*Diferencial do desafio. Descreve como um consumidor (pessoa ou integrador) se autentica, o que pode fazer e como o erro é tratado; e como o serviço protege identidade, transporte, segredos e dados.*

## Identidade e autorização

| Consumidor | Fluxo | Escopo |
|---|---|---|
| Pessoa (navegador) | OIDC **authorization code + PKCE** no Cognito | `lancamentos.write` para registrar e estornar; `consolidado.read` para consultar |
| Integrador (ERP, scripts) | OAuth 2.0 **client credentials** | Concedido por cliente; o mesmo par de escopos |

- O JWT é validado por **issuer**, **audience**, assinatura (chaves públicas via **JWKS**) e validade; a validação não confia em claims não assinados.
- O **`comercianteId` vem do token**, nunca do corpo. Um token pertence a um comerciante no MVP (multi-merchant é evolução); a consulta e o estorno só enxergam os dados daquele comerciante.
- O token tem TTL curto (o alvo: 15 min, com refresh para pessoas); a autorização é por escopo, não por papel.

## Transporte e borda

- **TLS obrigatório** no ALB (ACM); HSTS no domínio público.
- **WAF** com regras gerenciadas e **rate limit por cliente**; requisições acima do limite recebem 429.
- As tasks ficam em **sub-rede privada**; o tráfego de saída vai por NAT ou VPC endpoints (ADR-010).
- O banco e o broker não são expostos à internet; o acesso é por security group e credencial.

## Segredos e criptografia

- Senhas de banco e chaves no **Secrets Manager**, acessadas por **IAM role** da task; nada de segredo em código, imagem ou repositório.
- **KMS** para criptografia em repouso (RDS, MSK, volumes) e para os segredos.
- O JWT é assinado pelo Cognito; a chave de desenvolvimento do MVP (`Auth__SigningKey`) é **local e descartável** — o alvo não a usa.
- Rotação de segredos com versionamento no Secrets Manager; o `KMS` é o ponto de auditoria.

## Dados e privacidade

- **Em repouso:** RDS e MSK criptografados; o backup herda a criptografia.
- **Nos logs:** o `comercianteId` entra como **hash**; a `Idempotency-Key` e o corpo do lançamento só como hash/prefixo; não entram token, senha ou dado pessoal.
- **Trilha:** o lançamento é imutável; o estorno é um contra-lançamento; o `requestId`/`traceId` amarra log, requisição e evento.
- **Retenção:** logs 30 dias no alvo; a política de retenção das chaves de idempotência é definida junto com a operação (as chaves antigas podem ser arquivadas, não apagadas antes da janela de retry).

## Checklist do integrador

1. Obter o token por `client_credentials` no Cognito com os escopos necessários.
2. Chamar a API com `Authorization: Bearer <token>` e **`Idempotency-Key`** (UUID por operação) em todo POST.
3. Em retry, reenviar a **mesma chave e o mesmo corpo**: a resposta é a original.
4. Tratar os erros: `400` (contrato/header), `401` (token), `403` (escopo), `409` (concorrência ou estado), `422` (regra/validação), `429` (rate limit), `5xx` (retry com backoff exponencial e jitter).
5. Não reenviar com a mesma chave e corpo diferente: o serviço responde `422`.
6. Consultar o consolidado pelo dia; o dia é o do registro no fuso `America/Sao_Paulo`.

## Modelo de ameaças

| Ameaça | Controle |
|---|---|
| Replay de lançamento | Idempotência por chave + hash do corpo; unicidade no banco |
| Token roubado | TTL curto; TLS; escopo mínimo; revogação no Cognito; WAF/rate limit |
| Acesso a outro comerciante | `comercianteId` do token; consultas filtradas; sem id no corpo |
| Injeção SQL | SQL parametrizado (Dapper); sem concatenação de entrada |
| Evento forjado no Kafka | MSK privado; IAM; o consumidor valida schema e campos; o `eventId` é único |
| Evento duplicado/repetido | `evento_processado`; projeção idempotente |
| Negação por volume | WAF rate limit; ALB; escala horizontal; o consolidado lê projeção O(1) |
| Segredo no repositório | Secrets Manager; KMS; revisão de código; a chave de dev é local |

## O que o MVP não tem (e o alvo tem)

O MVP usa um **emissor JWT de desenvolvimento** (`/dev/token`) e um segredo local; o alvo usa Cognito, JWKS, Secrets Manager e KMS. O rate limit do WAF é do alvo; no local não há borda. A rotação de segredos e a revogação de tokens são operação do alvo, não do MVP. A autenticação de serviço a serviço no alvo é por IAM/roles das tasks, não por token de usuário.
