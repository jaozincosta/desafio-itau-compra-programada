using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Application.Services
{
    public class RebalanceamentoService : IRebalanceamentoService
    {
        private readonly IAppDbContext _context;
        private readonly ICotahistParser _parser;
        private readonly IKafkaProducer _kafka;
        private readonly IIRService _irService;
        private readonly string _pastaCotacoes;

        public RebalanceamentoService(
            IAppDbContext context,
            ICotahistParser parser,
            IKafkaProducer kafka,
            IIRService irService)
        {
            _context = context;
            _parser = parser;
            _kafka = kafka;
            _irService = irService;
            _pastaCotacoes = Path.Combine(Directory.GetCurrentDirectory(), "cotacoes");
        }

        //RN-045 a RN-049: Rebalanceamento por mudanca de cesta
        public async Task RebalancearPorMudancaCestaAsync(long cestaAntigaId, long cestaNovaId)
        {
            var cestaAntiga = await _context.CestasRecomendacao
                .Include(c => c.Itens)
                .FirstOrDefaultAsync(c => c.Id == cestaAntigaId);

            var cestaNova = await _context.CestasRecomendacao
                .Include(c => c.Itens)
                .FirstOrDefaultAsync(c => c.Id == cestaNovaId);

            if (cestaAntiga == null || cestaNova == null)
                throw new KeyNotFoundException("CESTA_NAO_ENCONTRADA");

            var tickersAntigos = cestaAntiga.Itens.Select(i => i.Ticker).ToHashSet();
            var tickersNovos = cestaNova.Itens.Select(i => i.Ticker).ToHashSet();

            var ativosRemovidos = tickersAntigos.Except(tickersNovos).ToList();
            var ativosAdicionados = tickersNovos.Except(tickersAntigos).ToList();

            var clientes = await _context.Clientes
                .Where(c => c.Ativo)
                .ToListAsync();

            var todosAtivos = ativosRemovidos.Concat(ativosAdicionados).Distinct().ToList();
            var cotacoes = _parser.ObterCotacoesFechamento(_pastaCotacoes, todosAtivos);

            foreach (var cliente in clientes)
            {
                var contaFilhote = await _context.ContasGraficas
                    .FirstOrDefaultAsync(cg => cg.ClienteId == cliente.Id && cg.Tipo == "FILHOTE");

                if (contaFilhote == null) continue;

                decimal valorDisponivel = 0;

                //vender ativos removidos
                foreach (var ticker in ativosRemovidos)
                {
                    var custodia = await _context.Custodias
                        .FirstOrDefaultAsync(c => c.ContaGraficaId == contaFilhote.Id
                            && c.Ticker == ticker && c.Quantidade > 0);

                    if (custodia == null) continue;

                    var cotacao = cotacoes.ContainsKey(ticker) ? cotacoes[ticker] : custodia.PrecoMedio;
                    var valorVenda = custodia.Quantidade * cotacao;
                    valorDisponivel += valorVenda;

                    //registrar rebalanceamento
                    _context.Rebalanceamentos.Add(new Rebalanceamento
                    {
                        ClienteId = cliente.Id,
                        Tipo = "MUDANCA_CESTA",
                        TickerVendido = ticker,
                        ValorVenda = valorVenda,
                        QuantidadeVendida = custodia.Quantidade,
                        DataRebalanceamento = DateTime.UtcNow
                    });

                    custodia.Quantidade = 0;
                    custodia.DataUltimaAtualizacao = DateTime.UtcNow;
                }

                //comprar ativos adicionados com valor obtido
                if (valorDisponivel > 0 && ativosAdicionados.Any())
                {
                    var valorPorAtivo = valorDisponivel / ativosAdicionados.Count;

                    foreach (var ticker in ativosAdicionados)
                    {
                        if (!cotacoes.ContainsKey(ticker)) continue;

                        var cotacao = cotacoes[ticker];
                        var quantidade = (int)Math.Truncate(valorPorAtivo / cotacao);

                        if (quantidade <= 0) continue;

                        var custodiaExistente = await _context.Custodias
                            .FirstOrDefaultAsync(c => c.ContaGraficaId == contaFilhote.Id
                                && c.Ticker == ticker);

                        if (custodiaExistente != null)
                        {
                            custodiaExistente.Quantidade += quantidade;
                            custodiaExistente.PrecoMedio = cotacao;
                            custodiaExistente.DataUltimaAtualizacao = DateTime.UtcNow;
                        }
                        else
                        {
                            _context.Custodias.Add(new Custodia
                            {
                                ContaGraficaId = contaFilhote.Id,
                                Ticker = ticker,
                                Quantidade = quantidade,
                                PrecoMedio = cotacao,
                                DataUltimaAtualizacao = DateTime.UtcNow
                            });
                        }

                        //registrar rebalanceamento de compra
                        _context.Rebalanceamentos.Add(new Rebalanceamento
                        {
                            ClienteId = cliente.Id,
                            Tipo = "MUDANCA_CESTA",
                            TickerComprado = ticker,
                            QuantidadeComprada = quantidade,
                            DataRebalanceamento = DateTime.UtcNow
                        });
                    }
                }

                //calcular e publicar IR sobre vendas do mes
                var agora = DateTime.UtcNow;
                var irVenda = await _irService.CalcularIRVendaAsync(
                    cliente.Id, agora.Month, agora.Year);

                if (irVenda > 0)
                {
                    _context.EventosIR.Add(new EventoIR
                    {
                        ClienteId = cliente.Id,
                        Tipo = "IR_VENDA",
                        ValorBase = valorDisponivel,
                        ValorIR = irVenda,
                        PublicadoKafka = false,
                        DataEvento = DateTime.UtcNow
                    });

                    try
                    {
                        var mensagem = JsonSerializer.Serialize(new
                        {
                            tipo = "IR_VENDA",
                            clienteId = cliente.Id,
                            cpf = cliente.Cpf,
                            mesReferencia = $"{agora.Year}-{agora.Month:D2}",
                            valorIR = irVenda,
                            dataCalculo = DateTime.UtcNow
                        });

                        await _kafka.PublicarAsync("ir-venda", mensagem);
                    }
                    catch (Exception) {}
                }
            }

            await _context.SaveChangesAsync();
        }

        //RN-050 a RN-052: Rebalanceamento por desvio de proporcao
        public async Task RebalancearPorDesvioAsync(long clienteId, decimal limiarDesvio = 5.0m)
        {
            var cesta = await _context.CestasRecomendacao
                .Include(c => c.Itens)
                .FirstOrDefaultAsync(c => c.Ativa);

            if (cesta == null)
                throw new KeyNotFoundException("CESTA_NAO_ENCONTRADA");

            var contaFilhote = await _context.ContasGraficas
                .FirstOrDefaultAsync(cg => cg.ClienteId == clienteId && cg.Tipo == "FILHOTE");

            if (contaFilhote == null)
                throw new KeyNotFoundException("CLIENTE_NAO_ENCONTRADO");

            var custodias = await _context.Custodias
                .Where(c => c.ContaGraficaId == contaFilhote.Id && c.Quantidade > 0)
                .ToListAsync();

            var tickers = cesta.Itens.Select(i => i.Ticker).ToList();
            var cotacoes = _parser.ObterCotacoesFechamento(_pastaCotacoes, tickers);

            //calcular valor total da carteira
            decimal valorTotal = 0;
            var valoresPorTicker = new Dictionary<string, decimal>();

            foreach (var custodia in custodias)
            {
                var cotacao = cotacoes.ContainsKey(custodia.Ticker)
                    ? cotacoes[custodia.Ticker] : custodia.PrecoMedio;
                var valor = custodia.Quantidade * cotacao;
                valoresPorTicker[custodia.Ticker] = valor;
                valorTotal += valor;
            }

            if (valorTotal <= 0) return;

            //verificar desvios e rebalancear
            foreach (var item in cesta.Itens)
            {
                var valorAtual = valoresPorTicker.ContainsKey(item.Ticker)
                    ? valoresPorTicker[item.Ticker] : 0;
                var proporcaoReal = (valorAtual / valorTotal) * 100;
                var desvio = proporcaoReal - item.Percentual;

                //se desvio maior que o limiar, precisa rebalancear
                if (Math.Abs(desvio) <= limiarDesvio) continue;

                var cotacao = cotacoes.ContainsKey(item.Ticker)
                    ? cotacoes[item.Ticker] : 0;
                if (cotacao <= 0) continue;

                var custodia = custodias.FirstOrDefault(c => c.Ticker == item.Ticker);

                if (desvio > 0)
                {
                    //sobre-alocado: vender excesso
                    var valorIdeal = valorTotal * (item.Percentual / 100);
                    var excessoValor = valorAtual - valorIdeal;
                    var qtdVender = (int)Math.Truncate(excessoValor / cotacao);

                    if (qtdVender > 0 && custodia != null)
                    {
                        custodia.Quantidade -= qtdVender;
                        custodia.DataUltimaAtualizacao = DateTime.UtcNow;

                        _context.Rebalanceamentos.Add(new Rebalanceamento
                        {
                            ClienteId = clienteId,
                            Tipo = "DESVIO",
                            TickerVendido = item.Ticker,
                            ValorVenda = qtdVender * cotacao,
                            QuantidadeVendida = qtdVender,
                            DataRebalanceamento = DateTime.UtcNow
                        });
                    }
                }
                else
                {
                    //sub-alocado: comprar deficit
                    var valorIdeal = valorTotal * (item.Percentual / 100);
                    var deficitValor = valorIdeal - valorAtual;
                    var qtdComprar = (int)Math.Truncate(deficitValor / cotacao);

                    if (qtdComprar > 0)
                    {
                        if (custodia != null)
                        {
                            custodia.Quantidade += qtdComprar;
                            custodia.PrecoMedio = cotacao;
                            custodia.DataUltimaAtualizacao = DateTime.UtcNow;
                        }
                        else
                        {
                            _context.Custodias.Add(new Custodia
                            {
                                ContaGraficaId = contaFilhote.Id,
                                Ticker = item.Ticker,
                                Quantidade = qtdComprar,
                                PrecoMedio = cotacao,
                                DataUltimaAtualizacao = DateTime.UtcNow
                            });
                        }

                        _context.Rebalanceamentos.Add(new Rebalanceamento
                        {
                            ClienteId = clienteId,
                            Tipo = "DESVIO",
                            TickerComprado = item.Ticker,
                            QuantidadeComprada = qtdComprar,
                            DataRebalanceamento = DateTime.UtcNow
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}