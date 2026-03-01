using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces;

namespace CompraProgramada.Infrastructure.Cotacoes
{
    public class CotahistParser : ICotahistParser
    {
        public IEnumerable<Cotacao> ParseArquivo(string caminhoArquivo)
        {
            var cotacoes = new List<Cotacao>();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var encoding = Encoding.GetEncoding("ISO-8859-1");

            foreach (var linha in File.ReadLines(caminhoArquivo, encoding))
            {
                if (linha.Length < 245)
                {
                    continue;
                }

                var tipoRegistro = linha.Substring(0, 2);
                if ( tipoRegistro != "01")
                {
                    continue;
                }

                var tipoMercado = int.Parse(linha.Substring(24, 3).Trim());

                // Filtrar apenas mercado a vista (010) e fracionario (020)
                if (tipoMercado != 10 && tipoMercado != 20)
                {
                    continue;
                }

                var codigoBdi = linha.Substring(10, 2).Trim();

                //Filtrar apenas lote padrao (02) e fracionario (96)
                if (codigoBdi != "02" && codigoBdi != "96")
                {
                    continue;
                }

                var cotacao = new Cotacao()
                {
                    DataPregao = DateTime.ParseExact(linha.Substring(2, 8), "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture),
                    Ticker = linha.Substring(12, 12).Trim(),
                    PrecoAbertura = ParsePreco(linha.Substring(56, 13)),
                    PrecoMaximo = ParsePreco(linha.Substring(69, 13)),
                    PrecoMinimo = ParsePreco(linha.Substring(82, 13)),
                    PrecoFechamento = ParsePreco(linha.Substring(108, 13))
                };

                cotacoes.Add(cotacao);
            }

            return cotacoes;
        }

        public Cotacao? ObterCotacaoFechamento(string pastaCotacoes, string ticker)
        {
            var arquivos = Directory.GetFiles(pastaCotacoes, "COTAHIST_D*.TXT")
                .OrderByDescending(f => f)
                .ToList();

            foreach (var  arquivo in arquivos)
            {
                var cotacoes = ParseArquivo(arquivo);
                var cotacao = cotacoes
                    .Where(c => c.Ticker.Equals(ticker, StringComparison.OrdinalIgnoreCase))
                    .Where(c => !c.Ticker.EndsWith("F"))
                    .FirstOrDefault();

                if (cotacao != null)
                {
                    return cotacao;
                }
            }

            return null;
        }

        public Dictionary<string, decimal> ObterCotacoesFechamento(string pastaCotacoes, IEnumerable<string> tickers)
        {
            var resultado = new Dictionary<string, decimal>();
            var tickersList = tickers.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var arquivos = Directory.GetFiles(pastaCotacoes, "COTAHIST_D*.TXT")
                .OrderByDescending(f => f)
                .ToList();

            foreach (var arquivo in arquivos)
            {
                var cotacoes = ParseArquivo(arquivo);
                foreach (var cotacao in cotacoes)
                {
                    if (tickersList.Contains(cotacao.Ticker) && !cotacao.Ticker.EndsWith("F") && !resultado.ContainsKey(cotacao.Ticker))
                    {
                        resultado[cotacao.Ticker] = cotacao.PrecoFechamento;
                    }
                }

                if (resultado.Count == tickersList.Count)
                {
                    break; //Todas as cotações necessarias foram encontradas
                }
            }

            return resultado;
        }

        private static decimal ParsePreco(string valorBruto)
        {
            if (long.TryParse(valorBruto.Trim(), out var valor))
            {
                return valor / 100m;
            }
            return 0;
        }
    }
}
