using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompraProgramada.Application.DTOs
{
    public record CestaRequest(string Nome, List<ItemCestaDto> Itens);

    public record ItemCestaDto(string Ticker, decimal Percentual);

    public record CestaResponse(
        long CestaId, string Nome, bool Ativa, DateTime DataCriacao,
        List<ItemCestaDto> Itens, bool RebalanceamentoDisparado, string Mensagem,
        CestaDesativadaDto? CestaAnteriorDesativada = null,
        List<string>? AtivosRemovidos = null,
        List<string>? AtivosAdicionados = null);

    public record CestaDesativadaDto(long CestaId, string Nome, DateTime DataDesativacao);

    public record CestaAtualResponse(
        long CestaId, string Nome, bool Ativa, DateTime DataCriacao,
        List<ItemCestaComCotacaoDto> Itens);

    public record ItemCestaComCotacaoDto(string Ticker, decimal Percentual, decimal? CotacaoAtual);

    public record HistoricoCestasResponse(List<CestaHistoricoDto> Cestas);

    public record CestaHistoricoDto(
        long CestaId, string Nome, bool Ativa, DateTime DataCriacao,
        DateTime? DataDesativacao, List<ItemCestaDto> Itens);
}