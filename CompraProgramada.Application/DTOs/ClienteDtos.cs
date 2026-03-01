using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompraProgramada.Application.DTOs
{
    public record AdesaoRequest(string Nome, string Cpf, string Email, decimal ValorMensal);

    public record AdesaoResponse(
        long ClienteId, string Nome, string Cpf, string Email,
        decimal ValorMensal, bool Ativo, DateTime DataAdesao,
        ContaGraficaDto ContaGrafica);

    public record ContaGraficaDto(long Id, string NumeroConta, string Tipo, DateTime DataCriacao);

    public record SaidaResponse(long ClienteId, string Nome, bool Ativo, DateTime DataSaida, string Mensagem);

    public record AlterarValorMensalRequest(decimal NovoValorMensal);

    public record AlterarValorMensalResponse(
        long ClienteId, decimal ValorMensalAnterior, decimal ValorMensalNovo,
        DateTime DataAlteracao, string Mensagem);
}
