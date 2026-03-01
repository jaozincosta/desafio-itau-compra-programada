using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Application.Services
{
    public class ClienteService : IClienteService
    {
        private readonly IAppDbContext _context;
        private readonly ICotahistParser _parser;
        private readonly string _pastaCotacoes;

        public ClienteService(IAppDbContext context, ICotahistParser parser)
        {
            _context = context;
            _parser = parser;
            _pastaCotacoes = Path.Combine(Directory.GetCurrentDirectory(), "cotacoes");
        }

        //RN-001 a RN-006: Adesao ao produto
        public async Task<AdesaoResponse> AderirAsync(AdesaoRequest request)
        {
            // RN-002: CPF unico
            var cpfExiste = await _context.Clientes.AnyAsync(c => c.Cpf == request.Cpf);
            if (cpfExiste)
                throw new InvalidOperationException("CLIENTE_CPF_DUPLICADO");

            // RN-003: Valor mensal minimo
            if (request.ValorMensal < 100)
                throw new InvalidOperationException("VALOR_MENSAL_INVALIDO");

            // RN-001: Criar cliente
            var cliente = new Cliente
            {
                Nome = request.Nome,
                Cpf = request.Cpf,
                Email = request.Email,
                ValorMensal = request.ValorMensal,
                Ativo = true,
                DataAdesao = DateTime.UtcNow
            };

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            // RN-004: Criar conta grafica filhote
            var contaGrafica = new ContaGrafica
            {
                ClienteId = cliente.Id,
                NumeroConta = $"FLH-{cliente.Id:D6}",
                Tipo = "FILHOTE",
                DataCriacao = DateTime.UtcNow
            };

            _context.ContasGraficas.Add(contaGrafica);
            await _context.SaveChangesAsync();

            return new AdesaoResponse(
                cliente.Id, cliente.Nome, cliente.Cpf, cliente.Email,
                cliente.ValorMensal, cliente.Ativo, cliente.DataAdesao,
                new ContaGraficaDto(contaGrafica.Id, contaGrafica.NumeroConta, contaGrafica.Tipo, contaGrafica.DataCriacao));
        }

        //RN-007 a RN-010: Saida do produto
        public async Task<SaidaResponse> SairAsync(long clienteId)
        {
            var cliente = await _context.Clientes.FindAsync(clienteId);
            if (cliente == null)
                throw new KeyNotFoundException("CLIENTE_NAO_ENCONTRADO");

            if (!cliente.Ativo)
                throw new InvalidOperationException("CLIENTE_JA_INATIVO");

            // RN-007: Mudar status para inativo
            cliente.Ativo = false;
            cliente.DataSaida = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // RN-008: Posicao na custodia e mantida
            return new SaidaResponse(
                cliente.Id, cliente.Nome, cliente.Ativo, cliente.DataSaida.Value,
                "Adesao encerrada. Sua posicao em custodia foi mantida.");
        }

        //RN-011 a RN-013: Alterar valor mensal
        public async Task<AlterarValorMensalResponse> AlterarValorMensalAsync(long clienteId, AlterarValorMensalRequest request)
        {
            var cliente = await _context.Clientes.FindAsync(clienteId);
            if (cliente == null)
                throw new KeyNotFoundException("CLIENTE_NAO_ENCONTRADO");

            if (request.NovoValorMensal < 100)
                throw new InvalidOperationException("VALOR_MENSAL_INVALIDO");

            var valorAnterior = cliente.ValorMensal;
            cliente.ValorMensal = request.NovoValorMensal;
            await _context.SaveChangesAsync();

            return new AlterarValorMensalResponse(
                cliente.Id, valorAnterior, cliente.ValorMensal,
                DateTime.UtcNow, "Valor mensal atualizado. O novo valor sera considerado a partir da proxima data de compra.");
        }

        //RN-063 a RN-070: Consultar carteira com P/L e rentabilidade
        public async Task<CarteiraResponse> ConsultarCarteiraAsync(long clienteId)
        {
            var cliente = await _context.Clientes
                .Include(c => c.ContaGrafica)
                .FirstOrDefaultAsync(c => c.Id == clienteId);

            if (cliente == null)
                throw new KeyNotFoundException("CLIENTE_NAO_ENCONTRADO");

            var contaGrafica = await _context.ContasGraficas
                .FirstOrDefaultAsync(cg => cg.ClienteId == clienteId && cg.Tipo == "FILHOTE");

            if (contaGrafica == null)
                throw new KeyNotFoundException("CLIENTE_NAO_ENCONTRADO");

            var custodias = await _context.Custodias
                .Where(c => c.ContaGraficaId == contaGrafica.Id && c.Quantidade > 0)
                .ToListAsync();

            // Buscar cotacoes atuais
            var tickers = custodias.Select(c => c.Ticker).ToList();
            var cotacoes = _parser.ObterCotacoesFechamento(_pastaCotacoes, tickers);

            var ativos = new List<AtivoCarteiraDto>();
            decimal valorTotalAtual = 0;
            decimal valorTotalInvestido = 0;

            foreach (var custodia in custodias)
            {
                var cotacaoAtual = cotacoes.ContainsKey(custodia.Ticker) ? cotacoes[custodia.Ticker] : custodia.PrecoMedio;
                var valorAtual = custodia.Quantidade * cotacaoAtual;
                var valorInvestido = custodia.Quantidade * custodia.PrecoMedio;
                var pl = valorAtual - valorInvestido;
                var plPercentual = valorInvestido > 0 ? Math.Round((pl / valorInvestido) * 100, 2) : 0;

                valorTotalAtual += valorAtual;
                valorTotalInvestido += valorInvestido;

                ativos.Add(new AtivoCarteiraDto(
                    custodia.Ticker, custodia.Quantidade, custodia.PrecoMedio,
                    cotacaoAtual, valorAtual, pl, plPercentual, 0));
            }

            // Calcular composicao percentual
            ativos = ativos.Select(a => a with
            {
                ComposicaoCarteira = valorTotalAtual > 0
                    ? Math.Round((a.ValorAtual / valorTotalAtual) * 100, 2)
                    : 0
            }).ToList();

            var plTotal = valorTotalAtual - valorTotalInvestido;
            var rentabilidade = valorTotalInvestido > 0
                ? Math.Round((plTotal / valorTotalInvestido) * 100, 2) : 0;

            return new CarteiraResponse(
                cliente.Id, cliente.Nome, contaGrafica.NumeroConta, DateTime.UtcNow,
                new ResumoCarteira(valorTotalInvestido, valorTotalAtual, plTotal, rentabilidade),
                ativos);
        }

        //Consultar rentabilidade detalhada com historico
        public async Task<RentabilidadeResponse> ConsultarRentabilidadeAsync(long clienteId)
        {
            var carteira = await ConsultarCarteiraAsync(clienteId);

            //Buscar historico de distribuicoes do cliente
            var contaGrafica = await _context.ContasGraficas
                .FirstOrDefaultAsync(cg => cg.ClienteId == clienteId && cg.Tipo == "FILHOTE");

            var distribuicoes = await _context.Distribuicoes
                .Where(d => d.CustodiaFilhoteId == contaGrafica!.Id)
                .OrderBy(d => d.DataDistribuicao)
                .ToListAsync();

            //Montar historico de aportes
            var historicoAportes = new List<HistoricoAporteDto>();
            var evolucaoCarteira = new List<EvolucaoCarteiraDto>();
            decimal acumuladoInvestido = 0;

            var distribuicoesPorData = distribuicoes
                .GroupBy(d => d.DataDistribuicao.Date)
                .OrderBy(g => g.Key);

            int parcelaNum = 0;
            foreach (var grupo in distribuicoesPorData)
            {
                parcelaNum++;
                var valorAporte = grupo.Sum(d => d.Quantidade * d.PrecoUnitario);
                acumuladoInvestido += valorAporte;

                int parcelaMes = ((parcelaNum - 1) % 3) + 1;
                historicoAportes.Add(new HistoricoAporteDto(grupo.Key, valorAporte, $"{parcelaMes}/3"));

                evolucaoCarteira.Add(new EvolucaoCarteiraDto(
                    grupo.Key, acumuladoInvestido, acumuladoInvestido, 0));
            }

            return new RentabilidadeResponse(
                carteira.ClienteId, carteira.Nome, DateTime.UtcNow,
                carteira.Resumo, historicoAportes, evolucaoCarteira);
        }
    }
}