using CompraProgramada.Application.Services;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CompraProgramada.Tests
{
    public class ContaMasterServiceTests
    {
        private AppDbContext CriarContexto()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task ConsultarCustodia_ContaMasterExiste_DeveRetornarCustodia()
        {
            var context = CriarContexto();

            context.ContasGraficas.Add(new ContaGrafica
            {
                Id = 1,
                ClienteId = null,
                NumeroConta = "MST-000001",
                Tipo = "MASTER",
                DataCriacao = DateTime.UtcNow
            });

            context.Custodias.Add(new Custodia
            {
                ContaGraficaId = 1,
                Ticker = "PETR4",
                Quantidade = 3,
                PrecoMedio = 35.00m,
                DataUltimaAtualizacao = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new ContaMasterService(context);
            var resultado = await service.ConsultarCustodiaAsync();

            Assert.Equal("MST-000001", resultado.ContaMaster.NumeroConta);
            Assert.Single(resultado.Custodia);
            Assert.Equal("PETR4", resultado.Custodia[0].Ticker);
            Assert.Equal(3, resultado.Custodia[0].Quantidade);
        }

        [Fact]
        public async Task ConsultarCustodia_SemContaMaster_DeveLancarExcecao()
        {
            var context = CriarContexto();
            var service = new ContaMasterService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => service.ConsultarCustodiaAsync());
        }

        [Fact]
        public async Task ConsultarCustodia_SemResiduos_DeveRetornarListaVazia()
        {
            var context = CriarContexto();

            context.ContasGraficas.Add(new ContaGrafica
            {
                Id = 1,
                ClienteId = null,
                NumeroConta = "MST-000001",
                Tipo = "MASTER",
                DataCriacao = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new ContaMasterService(context);
            var resultado = await service.ConsultarCustodiaAsync();

            Assert.Empty(resultado.Custodia);
            Assert.Equal(0, resultado.ValorTotalResiduo);
        }

        [Fact]
        public async Task ConsultarCustodia_DeveCalcularValorTotalResiduo()
        {
            var context = CriarContexto();

            context.ContasGraficas.Add(new ContaGrafica
            {
                Id = 1,
                ClienteId = null,
                NumeroConta = "MST-000001",
                Tipo = "MASTER",
                DataCriacao = DateTime.UtcNow
            });

            context.Custodias.Add(new Custodia
            {
                ContaGraficaId = 1,
                Ticker = "PETR4",
                Quantidade = 1,
                PrecoMedio = 35.00m,
                DataUltimaAtualizacao = DateTime.UtcNow
            });
            context.Custodias.Add(new Custodia
            {
                ContaGraficaId = 1,
                Ticker = "ITUB4",
                Quantidade = 1,
                PrecoMedio = 30.00m,
                DataUltimaAtualizacao = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new ContaMasterService(context);
            var resultado = await service.ConsultarCustodiaAsync();

            Assert.Equal(2, resultado.Custodia.Count);
            Assert.Equal(65.00m, resultado.ValorTotalResiduo);
        }
    }
}