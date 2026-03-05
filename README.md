# 💰 Compra Programada - Desafio Técnico Itaú Corretora

Sistema de compra programada de ações que automatiza investimentos de clientes em datas específicas (dias 5, 15 e 25 de cada mês), distribuindo proporcionalmente os aportes com base em uma cesta de recomendação (Top Five).

---

## 🏗️ Arquitetura

O projeto segue os princípios de **Clean Architecture** com separação clara de responsabilidades:
```
CompraProgramada.sln
├── CompraProgramada.Domain          → Entities, Enums e Interfaces (zero dependências externas)
├── CompraProgramada.Application     → DTOs, Services e regras de negócio
├── CompraProgramada.Infrastructure  → DbContext (MySQL), Kafka Producer e CotahistParser
├── CompraProgramada.API             → Controllers REST + Swagger
├── CompraProgramada.Worker          → Consumer Kafka para eventos de IR
└── CompraProgramada.Tests           → Testes unitários com xUnit + InMemory DB
```

**Fluxo de dependências:** `Domain` ← `Application` ← `Infrastructure` ← `API / Worker`

O projeto Application depende apenas de interfaces (`IAppDbContext`), nunca diretamente do Infrastructure. A inversão de dependência é resolvida via injeção no `Program.cs`.

---

## 🛠️ Tecnologias

| Tecnologia | Uso |
|---|---|
| .NET 8 (LTS) | Framework principal |
| MySQL 8.0 | Banco de dados relacional |
| Pomelo.EntityFrameworkCore | Provider MySQL para EF Core |
| Apache Kafka | Mensageria para eventos de IR |
| Docker Compose | Orquestração dos serviços |
| Swagger / OpenAPI | Documentação interativa da API |
| xUnit | Testes unitários |
| EF Core Migrations | Versionamento do schema |

---

## 🚀 Como Executar

### Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### 1. Clone o repositório
```bash
git clone https://github.com/jaozincosta/desafio-itau-compra-programada.git
cd desafio-itau-compra-programada
```

### 2. Suba os containers (MySQL + Kafka + Zookeeper)
```bash
docker compose up -d
```

### 3. Aplique as migrations
```bash
dotnet ef database update --project CompraProgramada.Infrastructure --startup-project CompraProgramada.API
```

### 4. Rode a API
```bash
dotnet run --project CompraProgramada.API --urls "http://localhost:5092"
```

Acesse o Swagger em: **http://localhost:5092**

### 5. Rode o Worker Kafka (em outro terminal)
```bash
dotnet run --project CompraProgramada.Worker
```

### 6. Rode os testes
```bash
dotnet test
```

---

## 📡 Endpoints da API

### Clientes

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `POST` | `/api/Clientes/adesao` | Aderir ao produto |
| `POST` | `/api/Clientes/{id}/saida` | Sair do produto |
| `PUT` | `/api/Clientes/{id}/valor-mensal` | Alterar valor mensal |
| `GET` | `/api/Clientes/{id}/carteira` | Consultar carteira com P/L |
| `GET` | `/api/Clientes/{id}/rentabilidade` | Consultar rentabilidade |

### Cesta de Recomendação (Admin)

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `POST` | `/api/admin/cesta` | Cadastrar/alterar cesta Top Five |
| `GET` | `/api/admin/cesta/atual` | Obter cesta ativa |
| `GET` | `/api/admin/cesta/historico` | Histórico de cestas |

### Motor de Compra

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `POST` | `/api/motor/executar-compra` | Executar compra programada |

### Conta Master (Admin)

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `GET` | `/api/admin/conta-master/custodia` | Consultar resíduos da conta master |

---

## ⚙️ Fluxo do Motor de Compra (18 etapas)
```
1.  Verificar se é dia de compra (5, 15 ou 25) e dia útil
2.  Buscar todos os clientes ativos
3.  Calcular 1/3 do valor mensal de cada cliente
4.  Obter cotações de fechamento via arquivo COTAHIST da B3
5.  Verificar saldo de resíduos na conta master
6.  Calcular quantidade a comprar por ativo (baseado na cesta)
7.  Registrar ordens de compra (lote padrão vs fracionário)
8.  Atualizar custódia da conta master
9.  Distribuir ações proporcionalmente entre clientes
10. Truncar quantidades (sem frações de ações)
11. Atualizar preço médio de cada cliente
12. Registrar distribuições
13. Calcular IR dedo-duro (0,005% por operação)
14. Publicar eventos de IR no Kafka
15. Descontar ações distribuídas da conta master
16. Persistir resíduos na conta master
17. Salvar estado final no banco
18. Retornar resumo da execução
```

---

## 📋 Regras de Negócio Implementadas

### Cliente (RN-001 a RN-013)
- CPF único por cliente
- Valor mensal mínimo de R$100
- Criação automática de conta gráfica filhote na adesão
- Saída mantém posição em custódia
- Alteração de valor reflete na próxima compra

### Cesta Top Five (RN-014 a RN-019)
- Exatamente 5 ativos por cesta
- Soma dos percentuais deve ser 100%
- Ao alterar, cesta anterior é desativada
- Mudança de cesta dispara rebalanceamento automático

### Motor de Compra (RN-020 a RN-044)
- Execução nos dias 5, 15 e 25 (1/3 do valor mensal por data)
- Consolidação de aportes e compra em lote pela conta master
- Separação automática entre lote padrão (múltiplos de 100) e fracionário
- Distribuição proporcional com truncamento (sem frações de ações)
- Resíduos permanecem na conta master para próxima compra
- Preço médio ponderado: `PM = (Qtd Anterior × PM Anterior + Qtd Nova × Preço Novo) / Qtd Total`

### IR (RN-053 a RN-062)
- Dedo-duro: 0,005% sobre o valor de cada operação
- Vendas ≤ R$20.000/mês: isentas de IR
- Vendas > R$20.000/mês: 20% sobre o lucro líquido
- Prejuízo não gera IR
- Eventos publicados no Kafka para processamento assíncrono

### Rebalanceamento (RN-045 a RN-052)
- Por mudança de cesta: vende ativos removidos, compra adicionados
- Por desvio de proporção: limiar de 5% de desvio da alocação ideal

---

## 🧠 Decisões Técnicas

| Decisão | Justificativa |
|---------|---------------|
| **Clean Architecture** | Separação clara entre regras de negócio e infraestrutura, facilitando testes e manutenção |
| **IAppDbContext (interface)** | Inversão de dependência — Application não referencia Infrastructure, permitindo testes com InMemory DB |
| **DeleteBehavior.Restrict** | Sistema financeiro exige controle explícito sobre exclusões para auditoria e compliance |
| **Kafka com Acks.All** | Garantia de entrega e idempotência para eventos fiscais de IR dedo-duro |
| **Conta Master nullable** | Conta da corretora (sem cliente associado) para acumular resíduos de distribuição |
| **Pomelo MySQL** | Provider mais maduro e mantido para MySQL + EF Core, com melhor suporte a tipos específicos |
| **Seed da Conta Master** | A conta master é criada automaticamente via migration, garantindo que o sistema funcione desde o primeiro uso |

---

## 🗃️ Estrutura do Banco de Dados

10 tabelas baseadas no diagrama ER fornecido:

| Tabela | Descrição |
|--------|-----------|
| `Clientes` | Dados do investidor + valor mensal |
| `ContasGraficas` | Contas master (corretora) e filhote (cliente) |
| `Custodias` | Posição por ativo por conta (índice único: conta + ticker) |
| `CestasRecomendacao` | Cestas Top Five (ativa/inativa) |
| `ItensCesta` | Ativos da cesta com percentual de alocação |
| `OrdensCompra` | Ordens executadas pela conta master |
| `Distribuicoes` | Distribuição de ações para contas filhote |
| `EventosIR` | Eventos de IR dedo-duro e venda |
| `Cotacoes` | Cotações históricas da B3 (COTAHIST) |
| `Rebalanceamentos` | Operações de rebalanceamento por mudança de cesta ou desvio |

---

## ✅ Testes

10 testes unitários cobrindo as regras de negócio mais críticas:

| Classe | Testes | Cobertura |
|--------|--------|-----------|
| `PrecoMedioServiceTests` | 5 | Primeira compra, média ponderada, acumulação, validações de entrada |
| `IRServiceTests` | 5 | Cálculo do dedo-duro com diferentes valores, valor zero |
```bash
dotnet test
# 10 passed, 0 failed
```
