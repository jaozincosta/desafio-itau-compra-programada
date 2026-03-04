using CompraProgramada.Application.Services;
using Xunit;

namespace CompraProgramada.Tests
{
    public class PrecoMedioServiceTests
    {
        private readonly PrecoMedioService _service = new();

        [Fact]
        public void PrimeiraCompra_RetornaPrecoNovo()
        {
            var resultado = _service.CalcularPrecoMedio(0, 0, 100, 38.50m);
            Assert.Equal(38.50m, resultado);
        }

        [Fact]
        public void SegundaCompra_CalculaMediaPonderada()
        {
            var resultado = _service.CalcularPrecoMedio(100, 38.50m, 50, 40.00m);
            Assert.Equal(39.0m, resultado);
        }

        [Fact]
        public void QuantidadeZero_LancaExcecao()
        {
            Assert.Throws<ArgumentException>(() =>
                _service.CalcularPrecoMedio(100, 38.50m, 0, 40.00m));
        }

        [Fact]
        public void PrecoZero_LancaExcecao()
        {
            Assert.Throws<ArgumentException>(() =>
                _service.CalcularPrecoMedio(100, 38.50m, 50, 0));
        }

        [Fact]
        public void TerceiraCompra_AcumulaCorreto()
        {
            var pmSegunda = _service.CalcularPrecoMedio(100, 38.50m, 50, 40.00m);
            var pmTerceira = _service.CalcularPrecoMedio(150, pmSegunda, 200, 35.00m);
            Assert.Equal(36.7143m, pmTerceira);
        }
    }
}