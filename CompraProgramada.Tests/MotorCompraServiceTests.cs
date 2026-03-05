using CompraProgramada.Application.Services;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CompraProgramada.Tests
{
    public class MotorCompraServiceTests
    {
        private AppDbContext CriarContexto()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private (MotorCompraService service, AppDbContext context) CriarMotor(
            Dictionary<string, decimal>? cotacoes = null)
        {
            var context = CriarContexto();

            var parserMock = new Mock<ICotahistParser>();
            var cotacoesDict = cotacoes ?? new Dictionary<string, decimal>
            {
                ["PETR4"] = 35.00m,
                ["VALE3"] = 62.00m,
                ["ITUB4"] = 30.00m,
                ["BBDC4"] = 15.00m,
                ["WEGE3"] = 40.00m
            };
            parserMock.Setup(p => p.ObterCotacoesFechamento(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                .Returns(cotacoesDict);

            var kafkaMock = new Mock<IKafkaProducer>();
            kafkaMock.Setup(k => k.PublicarAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var precoMedioService = new PrecoMedioService();
            var irService = new IRService(context);

            var service = new MotorCompraService(
                context, parserMock.Object, kafkaMock.Object,
                precoMedioService, irService);

            return (service, context);
        }

        private async Task SeedCestaEClientes(AppDbContext context,
            decimal valorMensalCliente1 = 3000m, decimal valorMensalCliente2 = 6000m)
        {
            context.ContasGraficas.Add(new ContaGrafica
            {
                Id = 1,
                ClienteId = null,
                NumeroConta = "MST-000001",
                Tipo = "MASTER",
                DataCriacao = DateTime.UtcNow
            });

            var cesta = new CestaRecomendacao
            {
                Nome = "Top Five Teste",
                Ativa = true,
                DataCriacao = DateTime.UtcNow
            };
            cesta.Itens.Add(new ItemCesta { Ticker = "PETR4", Percentual = 30 });
            cesta.Itens.Add(new ItemCesta { Ticker = "VALE3", Percentual = 25 });
            cesta.Itens.Add(new ItemCesta { Ticker = "ITUB4", Percentual = 20 });
            cesta.Itens.Add(new ItemCesta { Ticker = "BBDC4", Percentual = 15 });
            cesta.Itens.Add(new ItemCesta { Ticker = "WEGE3", Percentual = 10 });
            context.CestasRecomendacao.Add(cesta);

            var cliente1 = new Cliente
            {
                Nome = "Cliente A",
                Cpf = "11111111111",
                Email = "a@test.com",
                ValorMensal = valorMensalCliente1,
                Ativo = true,
                DataAdesao = DateTime.UtcNow
            };
            context.Clientes.Add(cliente1);
            await context.SaveChangesAsync();

            context.ContasGraficas.Add(new ContaGrafica
            {
                ClienteId = cliente1.Id,
                NumeroConta = "FLH-000001",
                Tipo = "FILHOTE",
                DataCriacao = DateTime.UtcNow
            });

            if (valorMensalCliente2 > 0)
            {
                var cliente2 = new Cliente
                {
                    Nome = "Cliente B",
                    Cpf = "22222222222",
                    Email = "b@test.com",
                    ValorMensal = valorMensalCliente2,
                    Ativo = true,
                    DataAdesao = DateTime.UtcNow
                };
                context.Clientes.Add(cliente2);
                await context.SaveChangesAsync();

                context.ContasGraficas.Add(new ContaGrafica
                {
                    ClienteId = cliente2.Id,
                    NumeroConta = "FLH-000002",
                    Tipo = "FILHOTE",
                    DataCriacao = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(20)]
        [InlineData(28)]
        public async Task ExecutarCompra_DiasInvalidos_DeveLancarExcecao(int dia)
        {
            var (service, context) = CriarMotor();
            await SeedCestaEClientes(context);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ExecutarCompraAsync(new DateTime(2026, 3, dia)));
            Assert.Equal("DATA_INVALIDA_COMPRA", ex.Message);
        }

        [Fact]
        public async Task ExecutarCompra_DeveCalcularUmTercoDoValorMensal()
        {
            var (service, context) = CriarMotor();
            await SeedCestaEClientes(context, 3000m, 0);

            var resultado = await service.ExecutarCompraAsync(new DateTime(2026, 3, 5));

            Assert.Equal(1000m, resultado.TotalConsolidado);
        }

        [Fact]
        public async Task ExecutarCompra_DeveConsolidarAportesDeTodosClientes()
        {
            var (service, context) = CriarMotor();
            await SeedCestaEClientes(context, 3000m, 6000m);

            var resultado = await service.ExecutarCompraAsync(new DateTime(2026, 3, 5));

            Assert.Equal(3000m, resultado.TotalConsolidado);
            Assert.Equal(2, resultado.TotalClientes);
        }

        [Fact]
        public async Task ExecutarCompra_DeveTruncarQuantidades()
        {
            var (service, context) = CriarMotor();
            await SeedCestaEClientes(context, 3000m, 6000m);

            var resultado = await service.ExecutarCompraAsync(new DateTime(2026, 3, 5));

            var petr4 = resultado.OrdensCompra.FirstOrDefault(o => o.Ticker == "PETR4");
            Assert.NotNull(petr4);
            Assert.Equal(25, petr4!.QuantidadeTotal);
        }

        [Fact]
        public async Task ExecutarCompra_DeveSepararLoteEFracionario()
        {
            var cotacoes = new Dictionary<string, decimal>
            {
                ["PETR4"] = 2.00m,
                ["VALE3"] = 2.00m,
                ["ITUB4"] = 2.00m,
                ["BBDC4"] = 2.00m,
                ["WEGE3"] = 2.00m
            };

            var (service, context) = CriarMotor(cotacoes);
            await SeedCestaEClientes(context, 30000m, 0);

            var resultado = await service.ExecutarCompraAsync(new DateTime(2026, 3, 5));

            var petr4 = resultado.OrdensCompra.FirstOrDefault(o => o.Ticker == "PETR4");
            Assert.NotNull(petr4);

            var lotePadrao = petr4!.Detalhes.FirstOrDefault(d => d.Tipo == "LOTE");
            Assert.NotNull(lotePadrao);
            Assert.True(lotePadrao!.Quantidade % 100 == 0);
        }

        [Fact]
        public async Task ExecutarCompra_MesmaData_DeveLancarExcecao()
        {
            var (service, context) = CriarMotor();
            await SeedCestaEClientes(context);

            await service.ExecutarCompraAsync(new DateTime(2026, 3, 5));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ExecutarCompraAsync(new DateTime(2026, 3, 5)));
            Assert.Equal("COMPRA_JA_EXECUTADA", ex.Message);
        }

        [Fact]
        public async Task ExecutarCompra_SemCestaAtiva_DeveLancarExcecao()
        {
            var (service, context) = CriarMotor();

            context.ContasGraficas.Add(new ContaGrafica
            {
                Id = 1,
                ClienteId = null,
                NumeroConta = "MST-000001",
                Tipo = "MASTER",
                DataCriacao = DateTime.UtcNow
            });
            var cliente = new Cliente
            {
                Nome = "Teste",
                Cpf = "99999999999",
                Email = "t@t.com",
                ValorMensal = 1000,
                Ativo = true,
                DataAdesao = DateTime.UtcNow
            };
            context.Clientes.Add(cliente);
            await context.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => service.ExecutarCompraAsync(new DateTime(2026, 3, 5)));
            Assert.Equal("CESTA_NAO_ENCONTRADA", ex.Message);
        }

        [Fact]
        public async Task ExecutarCompra_SemClientesAtivos_DeveLancarExcecao()
        {
            var (service, context) = CriarMotor();

            context.ContasGraficas.Add(new ContaGrafica
            {
                Id = 1,
                ClienteId = null,
                NumeroConta = "MST-000001",
                Tipo = "MASTER",
                DataCriacao = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ExecutarCompraAsync(new DateTime(2026, 3, 5)));
            Assert.Equal("NENHUM_CLIENTE_ATIVO", ex.Message);
        }

        [Fact]
        public async Task ExecutarCompra_DevePublicarEventosIRNoKafka()
        {
            var (service, context) = CriarMotor();
            await SeedCestaEClientes(context, 30000m, 0);

            var resultado = await service.ExecutarCompraAsync(new DateTime(2026, 3, 5));

            Assert.True(resultado.EventosIRPublicados > 0);
        }

        [Fact]
        public async Task ExecutarCompra_DeveGerarResiduosNaContaMaster()
        {
            var (service, context) = CriarMotor();
            await SeedCestaEClientes(context, 3000m, 6000m);

            var resultado = await service.ExecutarCompraAsync(new DateTime(2026, 3, 5));

            Assert.NotNull(resultado.ResiduosCustMaster);
        }
    }
}