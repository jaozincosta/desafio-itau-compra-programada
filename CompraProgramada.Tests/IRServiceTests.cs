using CompraProgramada.Application.Services;
using Xunit;

namespace CompraProgramada.Tests
{
    public class IRServiceTests
    {
        [Theory]
        [InlineData(10000, 0.50)]
        [InlineData(50000, 2.50)]
        [InlineData(100000, 5.00)]
        [InlineData(1000, 0.05)]
        public void DedoDuro_CalculaCorreto(decimal valorOperacao, decimal irEsperado)
        {
            var context = TestHelper.CriarContextoInMemory();
            var service = new IRService(context);

            var resultado = service.CalcularIRDedoDuro(valorOperacao);

            Assert.Equal(irEsperado, resultado);
        }

        [Fact]
        public void DedoDuro_ValorZero_RetornaZero()
        {
            var context = TestHelper.CriarContextoInMemory();
            var service = new IRService(context);

            var resultado = service.CalcularIRDedoDuro(0);

            Assert.Equal(0, resultado);
        }
    }
}