using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompraProgramada.Application.DTOs
{
    public record CustodiaMasterResponse(
        ContaMasterDto ContaMaster,
        List<CustodiaMasterItemDto> Custodia,
        decimal ValorTotalResiduo);

    public record ContaMasterDto(long Id, string NumeroConta, string Tipo);

    public record CustodiaMasterItemDto(
        string Ticker, int Quantidade, decimal PrecoMedio,
        decimal? ValorAtual, string? Origem);
}
