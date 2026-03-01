using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Domain.Interfaces
{
    public interface ICotahistParser
    {
        IEnumerable<Cotacao> ParseArquivo(string caminhoArquivo);
        Cotacao? ObterCotacaoFechamento(string pastaCotacoes, string ticker);
        Dictionary<string, decimal> ObterCotacoesFechamento(string pastaCotacoes, IEnumerable<string> tickers);
    }
}
