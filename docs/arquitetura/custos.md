# Estimativa de custos — alvo AWS

*Diferencial do desafio. A estimativa é uma **ordem de grandeza** para comparação de arquitetura, não uma fatura. Preços extraídos em 18/11/2026 (Research #0001) e conferidos nas páginas oficiais; a região assumida é **us-east-1**. Confirmar no AWS Pricing Calculator antes de contratar.*

## Premissas de uso

| Premissa | Valor |
|---|---|
| Região | us-east-1 |
| Comerciantes ativos | 1.000 |
| Lançamentos | 200 mil/mês (~6,7 mil/dia) |
| Pico de leitura do consolidado | 50 req/s (enunciado); média assumida de 10 req/s |
| Requisições ao consolidado | ~26 milhões/mês (média de 10 req/s) |
| Logs | ~5 GB/mês |
| Retenção de logs | 30 dias (CloudWatch) |
| Janela | 730 horas/mês |

## Custos mensais (us-east-1)

| Item | Cálculo | US$/mês |
|---|---|---|
| **MSK provisionado** — 3 brokers `kafka.m7g.large` | 3 × US$ 0,204/h × 730 h | 446,76 |
| MSK — armazenamento (100 GB) | 100 GB × US$ 0,10 | 10,00 |
| **ECS Fargate** — 4 tasks (2 por serviço), 0,5 vCPU e 1 GB | (0,5 × 0,04048 + 1 × 0,004446) × 730 × 4 | 72,08 |
| **RDS PostgreSQL** `db.m7g.large` Multi-AZ | 0,337/h × 730 h | 246,01 |
| RDS — gp3 Multi-AZ (100 GB) | 100 GB × US$ 0,23 | 23,00 |
| **ALB** — hora fixa | 0,0225/h × 730 h | 16,43 |
| ALB — LCU (média de 1) | 0,008 × 1 × 730 h | 5,84 |
| **WAF** — ACL + regras | 5,00 + 5 regras × 1,00 | 10,00 |
| WAF — requisições (26 M) | 26 × US$ 0,60/M | 15,60 |
| **NAT Gateway** — hora | 0,045/h × 730 h | 32,85 |
| NAT — dados processados (100 GB) | 100 GB × US$ 0,045 | 4,50 |
| **CloudWatch Logs** — ingestão (5 GB) | 5 GB × US$ 0,50 | 2,50 |
| CloudWatch — armazenamento (5 GB) e alarmes | 5 GB × US$ 0,03 + 10 alarmes × US$ 0,10 | 1,15 |
| **Secrets Manager** — 3 segredos | 3 × US$ 0,40 | 1,20 |
| **KMS** — 1 chave | US$ 1,00 | 1,00 |
| **Route 53** — hosted zone e consultas | 0,50 + ~0,40 | 0,90 |
| **Cognito** — até 50 mil MAU | faixa gratuita | 0,00 |
| **Total estimado** | | **≈ 890** |

## Alternativas e trade-offs de custo

| Alternativa | Impacto aproximado | O que se perde |
|---|---|---|
| **SQS FIFO** no lugar do MSK | −US$ 456 (o MSK vira poucos centavos de SQS) | Replay e lag por consumidor; a projeção deixa de ser reconstruível a partir do log; menor visibilidade operacional |
| **MSK Serverless** | +US$ 100 (piso de 0,75/cluster-h + partições) | Escala sem broker ligado; o piso domina em volume baixo |
| **MSK Express** | +US$ 447 | Mais throughput por broker e menos operação; caro demais para este volume |
| **RDS reservado 1 ano** | −US$ 57 (23% de desconto na instância) | Compromisso de 1 ano |
| **Fargate com Savings Plan** | −US$ 12 | Compromisso de 1 ano |
| **Separar a instância do consolidado** (ADR-0003) | +US$ 269 | Mais isolamento físico, dobro do custo de banco |
| **VPC endpoints** em vez de NAT | −US$ 37, com custo por endpoint | Mais configuração; sem saída geral para a internet |
| **Retenção de log menor** | −US$ 0,15 (armazenamento) | Menos histórico de investigação |

## Leituras

- O MSK é **mais da metade da fatura** e é o item que mais depende do requisito: se replay e lag não forem necessários, o SQS FIFO corta o custo pela metade. O [ADR-0002](../ultron/agents/adr/0002-kafka-kraft-barramento.md) registra por que o Kafka foi escolhido para o alvo.
- O RDS é o segundo maior item; a separação de instâncias (a evolução do ADR-0003) dobra esse custo — é uma decisão de disponibilidade, não de economia.
- A estimativa assume o pico de 50 req/s só nos dias de pico; se o pico for sustentado o mês inteiro, o WAF e o ALB sobem (as requisições ao WAF passariam de 26 M para ~131 M → +US$ 63).
- Aplicar Savings Plans e reservas é a alavanca mais simples depois de escolher o barramento.

## Fontes e ressalvas

- Preços extraídos das páginas oficiais de Fargate, MSK, RDS, ALB, WAF, NAT e CloudWatch em 18/11/2026 e registrados no [Research #0001](../ultron/agents/research/0001-fatos-2026.md), pergunta 10.
- O preço do RDS Multi-AZ veio de fontes que citam a Price List de 29/10/2026; a página oficial carrega a tabela dinamicamente — confirmar no Pricing Calculator.
- Região diferente de us-east-1 (por exemplo, sa-east-1) muda os valores; a estimativa não se transfere sem recálculo.
- Não estão incluídos: suporte AWS, licenças, tráfego de saída para a internet além do NAT, backups além da retenção gratuita do RDS e custos de desenvolvimento.
