using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompraProgramada.Application.DTOs
{
    public record ExecutarCompraRequest(DateTime DataReferencia);

    public record ExecutarCompraResponse(
        DateTime DataExecucao, int TotalClientes, decimal TotalConsolidado,
        List<OrdemCompraDto> OrdensCompra,
        List<DistribuicaoClienteDto> Distribuicoes,
        List<ResiduoDto> ResiduosCustMaster,
        int EventosIRPublicados, string Mensagem);

    public record OrdemCompraDto(
        string Ticker, int QuantidadeTotal,
        List<DetalheOrdemDto> Detalhes,
        decimal PrecoUnitario, decimal ValorTotal);

    public record DetalheOrdemDto(string Tipo, string Ticker, int Quantidade);

    public record DistribuicaoClienteDto(
        long ClienteId, string Nome, decimal ValorAporte,
        List<AtivoDistribuidoDto> Ativos);

    public record AtivoDistribuidoDto(string Ticker, int Quantidade);

    public record ResiduoDto(string Ticker, int Quantidade);
}