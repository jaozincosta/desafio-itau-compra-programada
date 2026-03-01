using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompraProgramada.Application.DTOs
{
    public record CarteiraResponse(
        long ClienteId, string Nome, string ContaGrafica, DateTime DataConsulta,
        ResumoCarteira Resumo, List<AtivoCarteiraDto> Ativos);

    public record ResumoCarteira(
        decimal ValorTotalInvestido, decimal ValorAtualCarteira,
        decimal PlTotal, decimal RentabilidadePercentual);

    public record AtivoCarteiraDto(
        string Ticker, int Quantidade, decimal PrecoMedio, decimal CotacaoAtual,
        decimal ValorAtual, decimal Pl, decimal PlPercentual, decimal ComposicaoCarteira);

    public record RentabilidadeResponse(
        long ClienteId, string Nome, DateTime DataConsulta,
        ResumoCarteira Rentabilidade,
        List<HistoricoAporteDto> HistoricoAportes,
        List<EvolucaoCarteiraDto> EvolucaoCarteira);

    public record HistoricoAporteDto(DateTime Data, decimal Valor, string Parcela);

    public record EvolucaoCarteiraDto(DateTime Data, decimal ValorCarteira, decimal ValorInvestido, decimal Rentabilidade);
}