using CompraProgramada.Application.Services;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CompraProgramada.Tests
{
    public class IRVendaServiceTests
    {
        private AppDbContext CriarContexto()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task CalcularIRVenda_VendasAbaixo20k_DeveRetornarZero()
        {
            var context = CriarContexto();
            var service = new IRService(context);

            var cliente = new Cliente
            {
                Nome = "Teste",
                Cpf = "11111111111",
                Email = "t@t.com",
                ValorMensal = 3000,
                Ativo = true,
                DataAdesao = DateTime.UtcNow
            };
            context.Clientes.Add(cliente);
            await context.SaveChangesAsync();

            var contaFilhote = new ContaGrafica
            {
                ClienteId = cliente.Id,
                NumeroConta = "FLH-000001",
                Tipo = "FILHOTE",
                DataCriacao = DateTime.UtcNow
            };
            context.ContasGraficas.Add(contaFilhote);

            context.Custodias.Add(new Custodia
            {
                ContaGraficaId = contaFilhote.Id,
                Ticker = "BBDC4",
                Quantidade = 100,
                PrecoMedio = 14.00m,
                DataUltimaAtualizacao = DateTime.UtcNow
            });

            context.Rebalanceamentos.Add(new Rebalanceamento
            {
                ClienteId = cliente.Id,
                Tipo = "MUDANCA_CESTA",
                TickerVendido = "BBDC4",
                ValorVenda = 1500m,
                QuantidadeVendida = 100,
                DataRebalanceamento = new DateTime(2026, 3, 1)
            });
            await context.SaveChangesAsync();

            var ir = await service.CalcularIRVendaAsync(cliente.Id, 3, 2026);
            Assert.Equal(0, ir);
        }

        [Fact]
        public async Task CalcularIRVenda_VendasAcima20k_ComLucro_DeveCalcular20Porcento()
        {
            var context = CriarContexto();
            var service = new IRService(context);

            var cliente = new Cliente
            {
                Nome = "Grande Investidor",
                Cpf = "22222222222",
                Email = "g@t.com",
                ValorMensal = 50000,
                Ativo = true,
                DataAdesao = DateTime.UtcNow
            };
            context.Clientes.Add(cliente);
            await context.SaveChangesAsync();

            var contaFilhote = new ContaGrafica
            {
                ClienteId = cliente.Id,
                NumeroConta = "FLH-000002",
                Tipo = "FILHOTE",
                DataCriacao = DateTime.UtcNow
            };
            context.ContasGraficas.Add(contaFilhote);

            context.Custodias.Add(new Custodia
            {
                ContaGraficaId = contaFilhote.Id,
                Ticker = "BBDC4",
                Quantidade = 0,
                PrecoMedio = 14.00m,
                DataUltimaAtualizacao = DateTime.UtcNow
            });

            context.Rebalanceamentos.Add(new Rebalanceamento
            {
                ClienteId = cliente.Id,
                Tipo = "MUDANCA_CESTA",
                TickerVendido = "BBDC4",
                ValorVenda = 8000m,
                QuantidadeVendida = 500,
                DataRebalanceamento = new DateTime(2026, 3, 10)
            });

            context.Custodias.Add(new Custodia
            {
                ContaGraficaId = contaFilhote.Id,
                Ticker = "WEGE3",
                Quantidade = 0,
                PrecoMedio = 38.00m,
                DataUltimaAtualizacao = DateTime.UtcNow
            });

            context.Rebalanceamentos.Add(new Rebalanceamento
            {
                ClienteId = cliente.Id,
                Tipo = "MUDANCA_CESTA",
                TickerVendido = "WEGE3",
                ValorVenda = 13500m,
                QuantidadeVendida = 300,
                DataRebalanceamento = new DateTime(2026, 3, 10)
            });
            await context.SaveChangesAsync();

            var ir = await service.CalcularIRVendaAsync(cliente.Id, 3, 2026);
            Assert.Equal(620m, ir);
        }

        [Fact]
        public async Task CalcularIRVenda_VendasAcima20k_ComPrejuizo_DeveRetornarZero()
        {
            var context = CriarContexto();
            var service = new IRService(context);

            var cliente = new Cliente
            {
                Nome = "Prejuizo",
                Cpf = "33333333333",
                Email = "p@t.com",
                ValorMensal = 50000,
                Ativo = true,
                DataAdesao = DateTime.UtcNow
            };
            context.Clientes.Add(cliente);
            await context.SaveChangesAsync();

            var contaFilhote = new ContaGrafica
            {
                ClienteId = cliente.Id,
                NumeroConta = "FLH-000003",
                Tipo = "FILHOTE",
                DataCriacao = DateTime.UtcNow
            };
            context.ContasGraficas.Add(contaFilhote);

            context.Custodias.Add(new Custodia
            {
                ContaGraficaId = contaFilhote.Id,
                Ticker = "PETR4",
                Quantidade = 0,
                PrecoMedio = 35.00m,
                DataUltimaAtualizacao = DateTime.UtcNow
            });

            context.Rebalanceamentos.Add(new Rebalanceamento
            {
                ClienteId = cliente.Id,
                Tipo = "MUDANCA_CESTA",
                TickerVendido = "PETR4",
                ValorVenda = 12800m,
                QuantidadeVendida = 400,
                DataRebalanceamento = new DateTime(2026, 3, 5)
            });

            context.Custodias.Add(new Custodia
            {
                ContaGraficaId = contaFilhote.Id,
                Ticker = "VALE3",
                Quantidade = 0,
                PrecoMedio = 55.00m,
                DataUltimaAtualizacao = DateTime.UtcNow
            });

            context.Rebalanceamentos.Add(new Rebalanceamento
            {
                ClienteId = cliente.Id,
                Tipo = "MUDANCA_CESTA",
                TickerVendido = "VALE3",
                ValorVenda = 11600m,
                QuantidadeVendida = 200,
                DataRebalanceamento = new DateTime(2026, 3, 5)
            });
            await context.SaveChangesAsync();

            var ir = await service.CalcularIRVendaAsync(cliente.Id, 3, 2026);
            Assert.Equal(0m, ir);
        }

        [Fact]
        public async Task CalcularIRVenda_SemVendasNoMes_DeveRetornarZero()
        {
            var context = CriarContexto();
            var service = new IRService(context);

            var ir = await service.CalcularIRVendaAsync(1, 3, 2026);
            Assert.Equal(0m, ir);
        }

        [Theory]
        [InlineData(280.00, 0.01)]
        [InlineData(248.00, 0.01)]
        [InlineData(0.01, 0.00)]
        public void DedoDuro_ValoresReais_CalculaCorreto(decimal valorOp, decimal irEsperado)
        {
            var context = CriarContexto();
            var service = new IRService(context);

            var resultado = service.CalcularIRDedoDuro(valorOp);
            Assert.Equal(irEsperado, resultado);
        }
    }
}