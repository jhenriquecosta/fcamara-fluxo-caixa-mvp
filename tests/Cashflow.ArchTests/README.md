# Cashflow.ArchTests

> A arquitetura escrita como teste: se alguém quebrar a direção das dependências ou contornar o mecanismo dos casos de uso, o build fica vermelho.

## Por que existe

Diagrama e ADR não impedem ninguém de adicionar uma referência errada às 18h de sexta. Um teste impede. Este projeto transforma as regras do ModularMonolith em verificações automáticas (NetArchTest), que rodam no CI junto com o resto.

## O que prova

**`Camadas`: a direção das dependências**

| Projeto | Não pode conhecer |
|---|---|
| CrossCuttingConcerns | nenhum outro projeto |
| Domain | Application, Contracts, Infrastructure, módulos, hosts |
| Application | Infrastructure, Contracts, módulos, hosts, Npgsql, Kafka |
| Contracts | nenhum outro projeto |
| Infrastructure | módulos, hosts |
| Cada módulo | o outro módulo, os hosts, Confluent.Kafka |

**`Modulos`: o mecanismo, não só os nomes das pastas**

- Todo `ICommand` tem handler e mora em `Commands`; toda `IQuery` tem handler e mora em `Queries`.
- Toda classe chamada `*Handler` implementa uma interface de handler.
- Endpoints, workers, consumidores e publicadores não dependem de handler concreto.
- Endpoints despacham pelo `Dispatcher`.

## Por que importa

A primeira versão da estrutura modular tinha as pastas da referência, mas nenhum módulo usava o `ICommand`, e os endpoints chamavam o handler concreto (ADR-0009). Estes testes existem para que isso não volte.
