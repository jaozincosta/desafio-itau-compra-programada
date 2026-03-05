using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Services;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CompraProgramada.Tests
{
    public class CestaServiceTests
    {
        private AppDbContext CriarContexto()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private ICestaService CriarService(AppDbContext context)
        {
            var rebalanceamentoMock = new Mock<IRebalanceamentoService>();
            return new CestaService(context, rebalanceamentoMock.Object);
        }

        [Fact]
        public async Task CadastrarCesta_Com5Ativos_DeveCriarComSucesso()
        {
            var context = CriarContexto();
            var service = CriarService(context);

            var request = new CestaRequest("Top Five Teste", new List<ItemCestaDto>
            {
                new("PETR4", 20), new("VALE3", 20), new("ITUB4", 20),
                new("BBDC4", 20), new("WEGE3", 20)
            });

            var resultado = await service.CadastrarOuAlterarAsync(request);

            Assert.True(resultado.Ativa);
            Assert.Equal(5, resultado.Itens.Count);
            Assert.Equal("Top Five Teste", resultado.Nome);
        }

        [Fact]
        public async Task CadastrarCesta_ComMenosDe5Ativos_DeveLancarExcecao()
        {
            var context = CriarContexto();
            var service = CriarService(context);

            var request = new CestaRequest("Invalida", new List<ItemCestaDto>
            {
                new("PETR4", 25), new("VALE3", 25), new("ITUB4", 25), new("BBDC4", 25)
            });

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CadastrarOuAlterarAsync(request));
            Assert.Equal("QUANTIDADE_ATIVOS_INVALIDA", ex.Message);
        }

        [Fact]
        public async Task CadastrarCesta_ComMaisDe5Ativos_DeveLancarExcecao()
        {
            var context = CriarContexto();
            var service = CriarService(context);

            var request = new CestaRequest("Invalida", new List<ItemCestaDto>
            {
                new("PETR4", 15), new("VALE3", 15), new("ITUB4", 15),
                new("BBDC4", 15), new("WEGE3", 15), new("ABEV3", 25)
            });

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CadastrarOuAlterarAsync(request));
            Assert.Equal("QUANTIDADE_ATIVOS_INVALIDA", ex.Message);
        }

        [Fact]
        public async Task CadastrarCesta_SomaPercentuaisDiferente100_DeveLancarExcecao()
        {
            var context = CriarContexto();
            var service = CriarService(context);

            var request = new CestaRequest("Invalida", new List<ItemCestaDto>
            {
                new("PETR4", 20), new("VALE3", 20), new("ITUB4", 20),
                new("BBDC4", 20), new("WEGE3", 10)
            });

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CadastrarOuAlterarAsync(request));
            Assert.Equal("PERCENTUAIS_INVALIDOS", ex.Message);
        }

        [Fact]
        public async Task AlterarCesta_DeveDesativarAnterior()
        {
            var context = CriarContexto();
            var service = CriarService(context);

            var request1 = new CestaRequest("Cesta V1", new List<ItemCestaDto>
            {
                new("PETR4", 20), new("VALE3", 20), new("ITUB4", 20),
                new("BBDC4", 20), new("WEGE3", 20)
            });
            await service.CadastrarOuAlterarAsync(request1);

            var request2 = new CestaRequest("Cesta V2", new List<ItemCestaDto>
            {
                new("PETR4", 25), new("VALE3", 25), new("ITUB4", 20),
                new("ABEV3", 15), new("RENT3", 15)
            });
            var cesta2 = await service.CadastrarOuAlterarAsync(request2);

            var cestasAtivas = await context.CestasRecomendacao
                .Where(c => c.Ativa).CountAsync();

            Assert.Equal(1, cestasAtivas);
            Assert.True(cesta2.Ativa);
            Assert.NotNull(cesta2.CestaAnteriorDesativada);
        }

        [Fact]
        public async Task ObterCestaAtual_SemCesta_DeveLancarExcecao()
        {
            var context = CriarContexto();
            var service = CriarService(context);

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => service.ObterAtualAsync());
            Assert.Equal("CESTA_NAO_ENCONTRADA", ex.Message);
        }

        [Fact]
        public async Task ObterHistorico_DeveRetornarTodasAsCestas()
        {
            var context = CriarContexto();
            var service = CriarService(context);

            var request = new CestaRequest("Cesta Historico", new List<ItemCestaDto>
            {
                new("PETR4", 20), new("VALE3", 20), new("ITUB4", 20),
                new("BBDC4", 20), new("WEGE3", 20)
            });
            await service.CadastrarOuAlterarAsync(request);

            var historico = await service.ObterHistoricoAsync();
            Assert.NotEmpty(historico.Cestas);
        }

        [Fact]
        public async Task AlterarCesta_DeveIdentificarAtivosRemovidosEAdicionados()
        {
            var context = CriarContexto();
            var service = CriarService(context);

            var request1 = new CestaRequest("V1", new List<ItemCestaDto>
            {
                new("PETR4", 20), new("VALE3", 20), new("ITUB4", 20),
                new("BBDC4", 20), new("WEGE3", 20)
            });
            await service.CadastrarOuAlterarAsync(request1);

            var request2 = new CestaRequest("V2", new List<ItemCestaDto>
            {
                new("PETR4", 20), new("VALE3", 20), new("ITUB4", 20),
                new("ABEV3", 20), new("RENT3", 20)
            });
            var resultado = await service.CadastrarOuAlterarAsync(request2);

            Assert.Contains("BBDC4", resultado.AtivosRemovidos!);
            Assert.Contains("WEGE3", resultado.AtivosRemovidos!);
            Assert.Contains("ABEV3", resultado.AtivosAdicionados!);
            Assert.Contains("RENT3", resultado.AtivosAdicionados!);
        }
    }
}