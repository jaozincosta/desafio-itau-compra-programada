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
└── CompraProgramada.Tests           → 53 testes unitários com xUnit + InMemory DB + Moq
```

**Fluxo de dependências:** `Domain` ← `Application` ← `Infrastructure` ← `API / Worker`

O projeto Application depende apenas de interfaces (`IAppDbContext`, `ICotahistParser`, `IKafkaProducer`), nunca diretamente do Infrastructure. A inversão de dependência é resolvida via injeção no `Program.cs`.

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
| xUnit + Moq | Testes unitários com mocking |
| EF Core InMemory | Banco em memória para testes |
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
# 53 passed, 0 failed
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

## 🔄 Fluxo de Demonstração (Swagger)

### 1. Cadastrar Cesta Top Five
```json
POST /api/admin/cesta
{
  "nome": "Top Five Março 2026",
  "itens": [
    { "ticker": "PETR4", "percentual": 20 },
    { "ticker": "VALE3", "percentual": 20 },
    { "ticker": "ITUB4", "percentual": 20 },
    { "ticker": "BBDC4", "percentual": 20 },
    { "ticker": "WEGE3", "percentual": 20 }
  ]
}
```

### 2. Aderir um Cliente
```json
POST /api/Clientes/adesao
{
  "nome": "João Costa",
  "email": "joao@gmail.com",
  "cpf": "12345678901",
  "valorMensal": 3000
}
```

### 3. Executar Motor de Compra
```json
POST /api/motor/executar-compra
{
  "dataReferencia": "2026-03-05T12:00:00Z"
}
```

### 4. Consultar Resultados
```
GET /api/Clientes/1/carteira
GET /api/Clientes/1/rentabilidade
GET /api/admin/conta-master/custodia
```

---

## ⚙️ Fluxo do Motor de Compra (18 etapas)
```
1.  Verificar se é dia de compra (5, 15 ou 25) ou próximo dia útil
2.  Buscar todos os clientes ativos
3.  Calcular 1/3 do valor mensal de cada cliente
4.  Obter cotações de fechamento via arquivo COTAHIST da B3
5.  Verificar saldo de resíduos na conta master
6.  Calcular quantidade a comprar por ativo (baseado na cesta)
7.  Registrar ordens de compra (lote padrão vs fracionário)
8.  Atualizar custódia da conta master (preço médio ponderado)
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
- Saída mantém posição em custódia (cliente pode continuar consultando)
- Alteração de valor reflete na próxima compra

### Cesta Top Five (RN-014 a RN-019)
- Exatamente 5 ativos por cesta
- Soma dos percentuais deve ser 100%
- Ao alterar, cesta anterior é desativada automaticamente
- Mudança de cesta dispara rebalanceamento automático
- Identifica ativos removidos e adicionados

### Motor de Compra (RN-020 a RN-044)
- Execução nos dias 5, 15 e 25 (1/3 do valor mensal por data)
- Se dia 5/15/25 cai no fim de semana, executa no próximo dia útil (RN-021)
- Consolidação de aportes e compra em lote pela conta master
- Separação automática entre lote padrão (múltiplos de 100) e fracionário
- Distribuição proporcional com truncamento (sem frações de ações)
- Resíduos permanecem na conta master para próxima compra
- Preço médio ponderado na conta master e nas custódias filhote
- Fórmula: `PM = (Qtd Anterior × PM Anterior + Qtd Nova × Preço Novo) / Qtd Total`

### IR (RN-053 a RN-062)
- Dedo-duro: 0,005% sobre o valor de cada operação
- Vendas ≤ R$20.000/mês: isentas de IR
- Vendas > R$20.000/mês: 20% sobre o lucro líquido
- Prejuízo não gera IR
- Eventos publicados no Kafka para processamento assíncrono

### Rebalanceamento (RN-045 a RN-052)
- Por mudança de cesta: vende ativos removidos, compra adicionados
- Por desvio de proporção: limiar de 5% de desvio da alocação ideal
- Cálculo de IR sobre vendas no rebalanceamento

---

## 🧠 Decisões Técnicas

| Decisão | Justificativa |
|---------|---------------|
| **Clean Architecture** | Separação clara entre regras de negócio e infraestrutura, facilitando testes e manutenção |
| **IAppDbContext (interface)** | Inversão de dependência — Application não referencia Infrastructure, permitindo testes com InMemory DB |
| **DeleteBehavior.Restrict** | Sistema financeiro exige controle explícito sobre exclusões para auditoria e compliance |
| **Kafka com Acks.All + Idempotência** | Garantia de entrega para eventos fiscais de IR dedo-duro |
| **Conta Master via Seed** | Criada automaticamente via migration, garantindo que o sistema funcione desde o primeiro uso |
| **Pomelo MySQL** | Provider mais maduro e mantido para MySQL + EF Core, com melhor suporte a tipos específicos |
| **Moq para testes** | Permite isolar services de dependências externas (Kafka, COTAHIST), garantindo testes rápidos e confiáveis |
| **InMemory DB nos testes** | Elimina necessidade de MySQL para rodar testes, facilitando CI/CD |
| **Preço médio ponderado na master** | Garante consistência no cálculo de resíduos entre ciclos de compra |

---

## 🗃️ Estrutura do Banco de Dados

10 tabelas com relacionamentos bem definidos:

| Tabela | Descrição |
|--------|-----------|
| `Clientes` | Dados do investidor + valor mensal (CPF com índice único) |
| `ContasGraficas` | Contas master (corretora) e filhote (cliente) |
| `Custodias` | Posição por ativo por conta (índice único: conta + ticker) |
| `CestasRecomendacao` | Cestas Top Five (ativa/inativa com histórico) |
| `ItensCesta` | Ativos da cesta com percentual de alocação |
| `OrdensCompra` | Ordens executadas pela conta master (lote/fracionário) |
| `Distribuicoes` | Distribuição de ações para contas filhote |
| `EventosIR` | Eventos de IR dedo-duro e venda (com flag Kafka) |
| `Cotacoes` | Cotações históricas da B3 (índice único: ticker + data) |
| `Rebalanceamentos` | Operações de rebalanceamento por mudança de cesta ou desvio |

---

## ✅ Testes

**53 testes unitários** cobrindo as regras de negócio críticas:

| Classe | Testes | Cobertura |
|--------|--------|-----------|
| `PrecoMedioServiceTests` | 5 | Primeira compra, média ponderada, acumulação, validações |
| `IRServiceTests` | 5 | Cálculo do dedo-duro com diferentes valores, valor zero |
| `IRVendaServiceTests` | 8 | Vendas abaixo/acima R$20k, lucro, prejuízo, isenção |
| `CestaServiceTests` | 8 | 5 ativos, soma 100%, desativar anterior, histórico, ativos removidos/adicionados |
| `ClienteServiceTests` | 12 | Adesão, CPF duplicado, valor mínimo, saída, inativação, alteração valor, carteira |
| `MotorCompraServiceTests` | 11 | Dias válidos/inválidos, 1/3 valor, consolidação, truncamento, lote/fracionário, Kafka, resíduos |
| `ContaMasterServiceTests` | 4 | Custódia, resíduos, valor total, conta inexistente |
```bash
dotnet test
# 53 passed, 0 failed
```

---

## 📂 Estrutura do Projeto
```
CompraProgramada/
├── CompraProgramada.API/
│   ├── Controllers/
│   │   ├── AdminCestaController.cs
│   │   ├── ClientesController.cs
│   │   ├── ContaMasterController.cs
│   │   └── MotorCompraController.cs
│   ├── cotacoes/
│   │   └── COTAHIST_D03032026.TXT
│   ├── Program.cs
│   └── appsettings.json
├── CompraProgramada.Application/
│   ├── DTOs/
│   │   ├── CarteiraDtos.cs
│   │   ├── CestaDtos.cs
│   │   ├── ClienteDtos.cs
│   │   ├── ContaMasterDtos.cs
│   │   ├── ErroDto.cs
│   │   └── MotorCompraDtos.cs
│   ├── Interfaces/
│   │   ├── IAppDbContext.cs
│   │   ├── ICestaService.cs
│   │   ├── IClienteService.cs
│   │   ├── IContaMasterService.cs
│   │   ├── IIRService.cs
│   │   ├── IMotorCompraService.cs
│   │   ├── IPrecoMedioService.cs
│   │   └── IRebalanceamentoService.cs
│   └── Services/
│       ├── CestaService.cs
│       ├── ClienteService.cs
│       ├── ContaMasterService.cs
│       ├── IRService.cs
│       ├── MotorCompraService.cs
│       ├── PrecoMedioService.cs
│       └── RebalanceamentoService.cs
├── CompraProgramada.Domain/
│   ├── Entities/
│   │   ├── CestaRecomendacao.cs
│   │   ├── Cliente.cs
│   │   ├── ContaGrafica.cs
│   │   ├── Cotacao.cs
│   │   ├── Custodia.cs
│   │   ├── Distribuicao.cs
│   │   ├── EventoIR.cs
│   │   ├── ItemCesta.cs
│   │   ├── OrdemCompra.cs
│   │   └── Rebalanceamento.cs
│   ├── Enums/
│   └── Interfaces/
│       ├── ICotahistParser.cs
│       └── IKafkaProducer.cs
├── CompraProgramada.Infrastructure/
│   ├── Cotacoes/CotahistParser.cs
│   ├── Data/AppDbContext.cs
│   ├── Kafka/KafkaProducerService.cs
│   └── Migrations/
├── CompraProgramada.Worker/
│   ├── Worker.cs
│   └── Program.cs
├── CompraProgramada.Tests/
│   ├── CestaServiceTests.cs
│   ├── ClienteServiceTests.cs
│   ├── ContaMasterServiceTests.cs
│   ├── IRServiceTests.cs
│   ├── IRVendaServiceTests.cs
│   ├── MotorCompraServiceTests.cs
│   ├── PrecoMedioServiceTests.cs
│   └── TestHelper.cs
├── docker-compose.yml
└── README.md
```

---

## 🐳 Docker Compose

O `docker-compose.yml` orquestra os seguintes serviços:

| Serviço | Porta | Descrição |
|---------|-------|-----------|
| MySQL 8.0 | 3307 | Banco de dados |
| Apache Kafka | 9092 | Mensageria |
| Zookeeper | 2181 | Coordenação Kafka |

---

## 📊 Códigos de Erro Padronizados

| Código HTTP | Código Erro | Descrição |
|---|---|---|
| 400 | CLIENTE_CPF_DUPLICADO | CPF já cadastrado no sistema |
| 400 | VALOR_MENSAL_INVALIDO | Valor mensal abaixo do mínimo (R$100) |
| 400 | PERCENTUAIS_INVALIDOS | Soma dos percentuais diferente de 100% |
| 400 | QUANTIDADE_ATIVOS_INVALIDA | Cesta não contém exatamente 5 ativos |
| 400 | CLIENTE_JA_INATIVO | Cliente já havia saído do produto |
| 404 | CLIENTE_NAO_ENCONTRADO | Cliente não encontrado |
| 404 | CESTA_NAO_ENCONTRADA | Nenhuma cesta ativa encontrada |
| 404 | COTACAO_NAO_ENCONTRADA | Arquivo COTAHIST não encontrado |
| 409 | COMPRA_JA_EXECUTADA | Compra já foi executada para esta data |
| 409 | DATA_INVALIDA_COMPRA | Data não é dia de compra válido |

---
