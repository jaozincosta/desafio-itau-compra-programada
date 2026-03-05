using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Application.Services
{
    public class MotorCompraService : IMotorCompraService
    {
        private readonly IAppDbContext _context;
        private readonly ICotahistParser _parser;
        private readonly IKafkaProducer _kafka;
        private readonly IPrecoMedioService _precoMedioService;
        private readonly IIRService _irService;
        private readonly string _pastaCotacoes;

        public MotorCompraService(
            IAppDbContext context,
            ICotahistParser parser,
            IKafkaProducer kafka,
            IPrecoMedioService precoMedioService,
            IIRService irService)
        {
            _context = context;
            _parser = parser;
            _kafka = kafka;
            _precoMedioService = precoMedioService;
            _irService = irService;
            _pastaCotacoes = Path.Combine(Directory.GetCurrentDirectory(), "cotacoes");
        }

        //RN-020 a RN-056: Motor de compra programada - 18 etapas
        public async Task<ExecutarCompraResponse> ExecutarCompraAsync(DateTime dataReferencia)
        {
            // ETAPA 1: Verificar se e dia de compra (5, 15 ou 25) ou proximo dia util
            var diasCompra = new[] { 5, 15, 25 };
            bool isDiaCompraValido = false;

            foreach (var diaCompra in diasCompra)
            {
                // Calcular o dia util correspondente ao dia de compra no mes da referencia
                var dataAlvo = new DateTime(dataReferencia.Year, dataReferencia.Month, diaCompra);
                while (dataAlvo.DayOfWeek == DayOfWeek.Saturday || dataAlvo.DayOfWeek == DayOfWeek.Sunday)
                    dataAlvo = dataAlvo.AddDays(1);

                if (dataReferencia.Date == dataAlvo.Date)
                {
                    isDiaCompraValido = true;
                    break;
                }
            }

            if (!isDiaCompraValido)
                throw new InvalidOperationException("DATA_INVALIDA_COMPRA");

            if (dataReferencia.DayOfWeek == DayOfWeek.Saturday || dataReferencia.DayOfWeek == DayOfWeek.Sunday)
                throw new InvalidOperationException("DATA_NAO_UTIL");

            //verificar se ja executou nesta data
            var jaExecutou = await _context.OrdensCompra
                .AnyAsync(o => o.DataExecucao.Date == dataReferencia.Date);
            if (jaExecutou)
                throw new InvalidOperationException("COMPRA_JA_EXECUTADA");

            //ETAPA 2: Buscar clientes ativos com valor mensal
            var clientes = await _context.Clientes
                .Where(c => c.Ativo)
                .ToListAsync();

            if (!clientes.Any())
                throw new InvalidOperationException("NENHUM_CLIENTE_ATIVO");

            //ETAPA 3: Calcular 1/3 do valor mensal de cada cliente
            var aportesPorCliente = clientes.ToDictionary(
                c => c.Id,
                c => Math.Round(c.ValorMensal / 3, 2));

            var totalConsolidado = aportesPorCliente.Values.Sum();

            //buscar cesta ativa
            var cesta = await _context.CestasRecomendacao
                .Include(c => c.Itens)
                .FirstOrDefaultAsync(c => c.Ativa);

            if (cesta == null)
                throw new KeyNotFoundException("CESTA_NAO_ENCONTRADA");

            //ETAPA 4: Obter cotacoes de fechamento
            var tickers = cesta.Itens.Select(i => i.Ticker).ToList();
            var cotacoes = _parser.ObterCotacoesFechamento(_pastaCotacoes, tickers);

            if (cotacoes.Count != tickers.Count)
                throw new KeyNotFoundException("COTACAO_NAO_ENCONTRADA");

            //buscar conta master
            var contaMaster = await _context.ContasGraficas
                .FirstOrDefaultAsync(cg => cg.Tipo == "MASTER");

            //ETAPA 5: Verificar saldo custodia master (residuos anteriores)
            var custodiasMaster = await _context.Custodias
                .Where(c => c.ContaGraficaId == contaMaster!.Id)
                .ToDictionaryAsync(c => c.Ticker, c => c);

            var ordensCompraDto = new List<OrdemCompraDto>();
            var ordensCompraEntities = new List<OrdemCompra>();
            var distribuicoesResponse = new List<DistribuicaoClienteDto>();
            var residuosFinais = new List<ResiduoDto>();
            int eventosIRPublicados = 0;

            foreach (var item in cesta.Itens)
            {
                var cotacao = cotacoes[item.Ticker];
                var valorParaAtivo = totalConsolidado * (item.Percentual / 100);

                //ETAPA 6: Calcular quantidade a comprar
                var quantidadeTotal = (int)Math.Truncate(valorParaAtivo / cotacao);
                var saldoMaster = custodiasMaster.ContainsKey(item.Ticker)
                    ? custodiasMaster[item.Ticker].Quantidade : 0;
                var quantidadeComprar = Math.Max(0, quantidadeTotal - saldoMaster);

                var quantidadeDisponivel = quantidadeComprar + saldoMaster;

                //ETAPA 7: Registrar ordens de compra (lote padrao vs fracionario)
                var detalhes = new List<DetalheOrdemDto>();

                if (quantidadeComprar > 0)
                {
                    var lotes = quantidadeComprar / 100;
                    var fracionario = quantidadeComprar % 100;

                    if (lotes > 0)
                    {
                        var ordemLote = new OrdemCompra
                        {
                            ContaMasterId = contaMaster!.Id,
                            Ticker = item.Ticker,
                            Quantidade = lotes * 100,
                            PrecoUnitario = cotacao,
                            TipoMercado = "LOTE",
                            DataExecucao = dataReferencia
                        };
                        _context.OrdensCompra.Add(ordemLote);
                        ordensCompraEntities.Add(ordemLote);
                        detalhes.Add(new DetalheOrdemDto("LOTE", item.Ticker, lotes * 100));
                    }

                    if (fracionario > 0)
                    {
                        var ordemFrac = new OrdemCompra
                        {
                            ContaMasterId = contaMaster!.Id,
                            Ticker = item.Ticker,
                            Quantidade = fracionario,
                            PrecoUnitario = cotacao,
                            TipoMercado = "FRACIONARIO",
                            DataExecucao = dataReferencia
                        };
                        _context.OrdensCompra.Add(ordemFrac);
                        ordensCompraEntities.Add(ordemFrac);
                        detalhes.Add(new DetalheOrdemDto("FRACIONARIO", $"{item.Ticker}F", fracionario));
                    }
                }

                //ETAPA 8: Atualizar custodia master com compras
                if (custodiasMaster.ContainsKey(item.Ticker))
                {
                    var custMaster = custodiasMaster[item.Ticker];
                    if (custMaster.Quantidade > 0 && quantidadeComprar > 0)
                    {
                        custMaster.PrecoMedio = _precoMedioService.CalcularPrecoMedio(
                            custMaster.Quantidade, custMaster.PrecoMedio,
                            quantidadeComprar, cotacao);
                    }
                    else if (quantidadeComprar > 0)
                    {
                        custMaster.PrecoMedio = cotacao;
                    }
                    custMaster.Quantidade = quantidadeDisponivel;
                    custMaster.DataUltimaAtualizacao = DateTime.UtcNow;
                }
                else
                {
                    var novaCustodia = new Custodia
                    {
                        ContaGraficaId = contaMaster!.Id,
                        Ticker = item.Ticker,
                        Quantidade = quantidadeDisponivel,
                        PrecoMedio = cotacao,
                        DataUltimaAtualizacao = DateTime.UtcNow
                    };
                    _context.Custodias.Add(novaCustodia);
                    custodiasMaster[item.Ticker] = novaCustodia;
                }

                // Salvar ordens para gerar os IDs antes da distribuicao
                await _context.SaveChangesAsync();

                //ETAPA 9 e 10: Distribuir para clientes proporcionalmente
                int totalDistribuido = 0;

                foreach (var cliente in clientes)
                {
                    var aporteCliente = aportesPorCliente[cliente.Id];
                    var proporcao = aporteCliente / totalConsolidado;

                    //Truncar a quantidade por cliente
                    var qtdCliente = (int)Math.Truncate(quantidadeDisponivel * proporcao);
                    if (qtdCliente <= 0) continue;

                    totalDistribuido += qtdCliente;

                    //Buscar ou criar custodia filhote
                    var contaFilhote = await _context.ContasGraficas
                        .FirstOrDefaultAsync(cg => cg.ClienteId == cliente.Id && cg.Tipo == "FILHOTE");

                    if (contaFilhote == null) continue;

                    var custodiaFilhote = await _context.Custodias
                        .FirstOrDefaultAsync(c => c.ContaGraficaId == contaFilhote.Id
                            && c.Ticker == item.Ticker);

                    //ETAPA 11: Atualizar preco medio do cliente
                    if (custodiaFilhote != null)
                    {
                        custodiaFilhote.PrecoMedio = _precoMedioService.CalcularPrecoMedio(
                            custodiaFilhote.Quantidade, custodiaFilhote.PrecoMedio,
                            qtdCliente, cotacao);
                        custodiaFilhote.Quantidade += qtdCliente;
                        custodiaFilhote.DataUltimaAtualizacao = DateTime.UtcNow;
                    }
                    else
                    {
                        custodiaFilhote = new Custodia
                        {
                            ContaGraficaId = contaFilhote.Id,
                            Ticker = item.Ticker,
                            Quantidade = qtdCliente,
                            PrecoMedio = cotacao,
                            DataUltimaAtualizacao = DateTime.UtcNow
                        };
                        _context.Custodias.Add(custodiaFilhote);
                    }

                    //ETAPA 12: Registrar distribuicao
                    var distribuicao = new Distribuicao
                    {
                        OrdemCompraId = ordensCompraEntities.LastOrDefault()?.Id ?? 0,
                        CustodiaFilhoteId = contaFilhote.Id,
                        Ticker = item.Ticker,
                        Quantidade = qtdCliente,
                        PrecoUnitario = cotacao,
                        DataDistribuicao = dataReferencia
                    };
                    _context.Distribuicoes.Add(distribuicao);

                    //ETAPA 13 e 14: Calcular e publicar IR dedo-duro
                    var valorOperacao = qtdCliente * cotacao;
                    var valorIR = _irService.CalcularIRDedoDuro(valorOperacao);

                    var eventoIR = new EventoIR
                    {
                        ClienteId = cliente.Id,
                        Tipo = "DEDO_DURO",
                        ValorBase = valorOperacao,
                        ValorIR = valorIR,
                        PublicadoKafka = false,
                        DataEvento = dataReferencia
                    };
                    _context.EventosIR.Add(eventoIR);

                    //Publicar no Kafka
                    try
                    {
                        var mensagemKafka = JsonSerializer.Serialize(new
                        {
                            tipo = "IR_DEDO_DURO",
                            clienteId = cliente.Id,
                            cpf = cliente.Cpf,
                            ticker = item.Ticker,
                            tipoOperacao = "COMPRA",
                            quantidade = qtdCliente,
                            precoUnitario = cotacao,
                            valorOperacao = valorOperacao,
                            aliquota = 0.00005m,
                            valorIR = valorIR,
                            dataOperacao = dataReferencia
                        });

                        await _kafka.PublicarAsync("ir-dedo-duro", mensagemKafka);
                        eventoIR.PublicadoKafka = true;
                        eventosIRPublicados++;
                    }
                    catch (Exception) { }
                }

                //ETAPA 15 e 16: Descontar distribuidos e persistir residuos
                var residuo = quantidadeDisponivel - totalDistribuido;
                custodiasMaster[item.Ticker].Quantidade = residuo;

                if (residuo > 0)
                    residuosFinais.Add(new ResiduoDto(item.Ticker, residuo));

                ordensCompraDto.Add(new OrdemCompraDto(
                    item.Ticker, quantidadeComprar, detalhes, cotacao,
                    quantidadeComprar * cotacao));
            }

            foreach (var cliente in clientes)
            {
                var contaFilhote = await _context.ContasGraficas
                    .FirstOrDefaultAsync(cg => cg.ClienteId == cliente.Id && cg.Tipo == "FILHOTE");

                if (contaFilhote == null) continue;

                var custodias = await _context.Custodias
                    .Where(c => c.ContaGraficaId == contaFilhote.Id && c.Quantidade > 0)
                    .ToListAsync();

                var ativosDistribuidos = custodias.Select(c =>
                    new AtivoDistribuidoDto(c.Ticker, c.Quantidade)).ToList();

                if (ativosDistribuidos.Any())
                {
                    distribuicoesResponse.Add(new DistribuicaoClienteDto(
                        cliente.Id, cliente.Nome, aportesPorCliente[cliente.Id],
                        ativosDistribuidos));
                }
            }

            //ETAPA 17: Persistir estado final
            await _context.SaveChangesAsync();

            //ETAPA 18: Compra concluida
            return new ExecutarCompraResponse(
                dataReferencia, clientes.Count, totalConsolidado,
                ordensCompraDto, distribuicoesResponse, residuosFinais,
                eventosIRPublicados,
                $"Compra programada executada com sucesso para {clientes.Count} clientes.");
        }
    }
}