using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Services;
using CompraProgramada.Domain.Interfaces;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CompraProgramada.Tests
{
    public class ClienteServiceTests
    {
        private AppDbContext CriarContexto()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private ClienteService CriarService(AppDbContext context)
        {
            var parserMock = new Mock<ICotahistParser>();
            parserMock.Setup(p => p.ObterCotacoesFechamento(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                .Returns(new Dictionary<string, decimal>());
            return new ClienteService(context, parserMock.Object);
        }

        [Fact]
        public async Task Aderir_DadosValidos_DeveCriarClienteEContaFilhote()
        {
            var context = CriarContexto();
            var service = CriarService(context);

            var request = new AdesaoRequest("Joao Silva", "12345678901", "joao@email.com", 3000);
            var resultado = await service.AderirAsync(request);

            Assert.Equal("Joao Silva", resultado.Nome);
            Assert.Equal("12345678901", resultado.Cpf);
            Assert.True(resultado.Ativo);
            Assert.Equal("FILHOTE", resultado.ContaGrafica.Tipo);
            Assert.StartsWith("FLH-", resultado.ContaGrafica.NumeroConta);
        }

        [Fact]
        public async Task Aderir_CpfDuplicado_DeveLancarExcecao()
        {
            var context = CriarContexto();
            var service = CriarService(context);

            var request = new AdesaoRequest("Joao", "12345678901", "joao@email.com", 3000);
            await service.AderirAsync(request);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.AderirAsync(request));
            Assert.Equal("CLIENTE_CPF_DUPLICADO", ex.Message);
        }

        [Fact]
        public async Task Aderir_ValorMensalAbaixoMinimo_DeveLancarExcecao()
        {
            var context = CriarContexto();
            var service = CriarService(context);

            var request = new AdesaoRequest("Maria", "98765432100", "maria@email.com", 50);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.AderirAsync(request));
            Assert.Equal("VALOR_MENSAL_INVALIDO", ex.Message);
        }

        [Fact]
        public async Task Aderir_ValorMensalExatamente100_DeveAceitar()
        {
            var context = CriarContexto();
            var service = CriarService(context);

            var request = new AdesaoRequest("Pedro", "11122233344", "pedro@email.com", 100);
            var resultado = await service.AderirAsync(request);

            Assert.Equal(100, resultado.ValorMensal);
        }

        [Fact]
        public async Task Sair_ClienteAtivo_DeveInativar()
        {
            var context = CriarContexto();
            var service = CriarService(context);

            var request = new AdesaoRequest("Ana", "55566677788", "ana@email.com", 500);
            var adesao = await service.AderirAsync(request);

            var saida = await service.SairAsync(adesao.ClienteId);

            Assert.False(saida.Ativo);
            Assert.Contains("posicao em custodia foi mantida", saida.Mensagem);
        }

        [Fact]
        public async Task Sair_ClienteInexistente_DeveLancarExcecao()
        {
            var context = CriarContexto();
            var service = CriarService(context);

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => service.SairAsync(999));
            Assert.Equal("CLIENTE_NAO_ENCONTRADO", ex.Message);
        }

        [Fact]
        public async Task Sair_ClienteJaInativo_DeveLancarExcecao()
        {
            var context = CriarContexto();
            var service = CriarService(context);

            var request = new AdesaoRequest("Carlos", "99988877766", "carlos@email.com", 200);
            var adesao = await service.AderirAsync(request);
            await service.SairAsync(adesao.ClienteId);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.SairAsync(adesao.ClienteId));
            Assert.Equal("CLIENTE_JA_INATIVO", ex.Message);
        }

        [Fact]
        public async Task AlterarValorMensal_DeveAtualizarValor()
        {
            var context = CriarContexto();
            var service = CriarService(context);

            var request = new AdesaoRequest("Lucas", "44455566677", "lucas@email.com", 1000);
            var adesao = await service.AderirAsync(request);

            var alteracao = await service.AlterarValorMensalAsync(
                adesao.ClienteId, new AlterarValorMensalRequest(3000));

            Assert.Equal(1000, alteracao.ValorMensalAnterior);
            Assert.Equal(3000, alteracao.ValorMensalNovo);
        }

        [Fact]
        public async Task AlterarValorMensal_AbaixoMinimo_DeveLancarExcecao()
        {
            var context = CriarContexto();
            var service = CriarService(context);

            var request = new AdesaoRequest("Julia", "33344455566", "julia@email.com", 500);
            var adesao = await service.AderirAsync(request);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.AlterarValorMensalAsync(
                    adesao.ClienteId, new AlterarValorMensalRequest(50)));
            Assert.Equal("VALOR_MENSAL_INVALIDO", ex.Message);
        }

        [Fact]
        public async Task ConsultarCarteira_ClienteInativo_DeveRetornarCarteira()
        {
            var context = CriarContexto();
            var service = CriarService(context);

            var request = new AdesaoRequest("Roberto", "22233344455", "roberto@email.com", 600);
            var adesao = await service.AderirAsync(request);
            await service.SairAsync(adesao.ClienteId);

            var carteira = await service.ConsultarCarteiraAsync(adesao.ClienteId);

            Assert.Equal(adesao.ClienteId, carteira.ClienteId);
            Assert.Empty(carteira.Ativos);
        }

        [Fact]
        public async Task ConsultarCarteira_ClienteInexistente_DeveLancarExcecao()
        {
            var context = CriarContexto();
            var service = CriarService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => service.ConsultarCarteiraAsync(999));
        }
    }
}