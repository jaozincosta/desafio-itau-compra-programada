using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Application.Services
{
    public class CestaService : ICestaService
    {
        private readonly IAppDbContext _context;
        private readonly IRebalanceamentoService _rebalanceamentoService;

        public CestaService(IAppDbContext context, IRebalanceamentoService rebalanceamentoService)
        {
            _context = context;
            _rebalanceamentoService = rebalanceamentoService;
        }

        //RN-014 a RN-019: Cadastrar ou alterar cesta Top Five
        public async Task<CestaResponse> CadastrarOuAlterarAsync(CestaRequest request)
        {
            //RN-015: Deve ter exatamente 5 ativos
            if (request.Itens.Count != 5)
                throw new InvalidOperationException("QUANTIDADE_ATIVOS_INVALIDA");

            //RN-016: Soma dos percentuais deve ser 100%
            var somaPercentuais = request.Itens.Sum(i => i.Percentual);
            if (somaPercentuais != 100)
                throw new InvalidOperationException("PERCENTUAIS_INVALIDOS");

            //Buscar cesta ativa atual
            var cestaAtual = await _context.CestasRecomendacao
                .Include(c => c.Itens)
                .FirstOrDefaultAsync(c => c.Ativa);

            CestaDesativadaDto? cestaDesativada = null;
            List<string>? ativosRemovidos = null;
            List<string>? ativosAdicionados = null;
            bool rebalanceamentoDisparado = false;

            if (cestaAtual != null)
            {
                var tickersAntigos = cestaAtual.Itens.Select(i => i.Ticker).ToHashSet();
                var tickersNovos = request.Itens.Select(i => i.Ticker).ToHashSet();

                ativosRemovidos = tickersAntigos.Except(tickersNovos).ToList();
                ativosAdicionados = tickersNovos.Except(tickersAntigos).ToList();

                //RN-017: Desativar cesta anterior
                cestaAtual.Ativa = false;
                cestaAtual.DataDesativacao = DateTime.UtcNow;

                cestaDesativada = new CestaDesativadaDto(
                    cestaAtual.Id, cestaAtual.Nome, cestaAtual.DataDesativacao.Value);
            }

            //RN-014: Criar nova cesta
            var novaCesta = new CestaRecomendacao
            {
                Nome = request.Nome,
                Ativa = true,
                DataCriacao = DateTime.UtcNow
            };

            foreach (var item in request.Itens)
            {
                novaCesta.Itens.Add(new ItemCesta
                {
                    Ticker = item.Ticker,
                    Percentual = item.Percentual
                });
            }

            _context.CestasRecomendacao.Add(novaCesta);
            await _context.SaveChangesAsync();

            //RN-018: Disparar rebalanceamento se havia cesta anterior com mudancas
            if (cestaAtual != null && (ativosRemovidos!.Any() || ativosAdicionados!.Any()))
            {
                try
                {
                    await _rebalanceamentoService.RebalancearPorMudancaCestaAsync(
                        cestaAtual.Id, novaCesta.Id);
                    rebalanceamentoDisparado = true;
                }
                catch (Exception)
                {
                    rebalanceamentoDisparado = false;
                }
            }

            return new CestaResponse(
                novaCesta.Id, novaCesta.Nome, novaCesta.Ativa, novaCesta.DataCriacao,
                request.Itens, rebalanceamentoDisparado,
                rebalanceamentoDisparado
                    ? "Cesta atualizada. Rebalanceamento disparado automaticamente."
                    : "Cesta cadastrada com sucesso.",
                cestaDesativada, ativosRemovidos, ativosAdicionados);
        }

        //Obter cesta ativa atual com cotacoes
        public async Task<CestaAtualResponse> ObterAtualAsync()
        {
            var cesta = await _context.CestasRecomendacao
                .Include(c => c.Itens)
                .FirstOrDefaultAsync(c => c.Ativa);

            if (cesta == null)
                throw new KeyNotFoundException("CESTA_NAO_ENCONTRADA");

            var itens = cesta.Itens.Select(i => new ItemCestaComCotacaoDto(
                i.Ticker, i.Percentual, null)).ToList();

            return new CestaAtualResponse(
                cesta.Id, cesta.Nome, cesta.Ativa, cesta.DataCriacao, itens);
        }

        //Historico de todas as cestas
        public async Task<HistoricoCestasResponse> ObterHistoricoAsync()
        {
            var cestas = await _context.CestasRecomendacao
                .Include(c => c.Itens)
                .OrderByDescending(c => c.DataCriacao)
                .ToListAsync();

            var cestasDto = cestas.Select(c => new CestaHistoricoDto(
                c.Id, c.Nome, c.Ativa, c.DataCriacao, c.DataDesativacao,
                c.Itens.Select(i => new ItemCestaDto(i.Ticker, i.Percentual)).ToList()
            )).ToList();

            return new HistoricoCestasResponse(cestasDto);
        }
    }
}