using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompraProgramada.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Application.Services
{
    public class IRService : IIRService
    {
        private readonly IAppDbContext _context;
        private const decimal ALIQUOTA_DEDO_DURO = 0.00005m;
        private const decimal ALIQUOTA_IR_VENDA = 0.20m;
        private const decimal LIMITE_ISENCAO = 20000m;

        public IRService(IAppDbContext context)
        {
            _context = context;
        }

        //RN-053: IR Dedo-Duro = 0,005% sobre o valor total da operacao
        public decimal CalcularIRDedoDuro(decimal valorOperacao)
        {
            var ir = valorOperacao * ALIQUOTA_DEDO_DURO;
            return Math.Round(ir, 2);
        }

        //RN-057 a RN-062: Calcula IR sobre vendas no mes
        //Se total vendas menor ou igual a R$ 20.000: ISENTO
        //Se total vendas maior que R$ 20.000: 20% sobre lucro liquido
        public async Task<decimal> CalcularIRVendaAsync(long clienteId, int mesReferencia, int anoReferencia)
        {
            //Buscar todos os rebalanceamentos com venda do cliente no mes
            var rebalanceamentos = await _context.Rebalanceamentos
                .Where(r => r.ClienteId == clienteId
                    && r.DataRebalanceamento.Month == mesReferencia
                    && r.DataRebalanceamento.Year == anoReferencia
                    && r.TickerVendido != null)
                .ToListAsync();
            
            if (!rebalanceamentos.Any())
            {
                return 0;
            }

            //RN-057: Somar todas as vendas do cliente no mes
            var totalVendas = rebalanceamentos.Sum(r => r.ValorVenda);

            //RN-058: Isento se vendas <= R$ 20.000
            if (totalVendas <= LIMITE_ISENCAO)
            {
                return 0;
            }

            //RN-059: Calcular lucro por operacao
            decimal lucroTotal = 0;

            foreach (var reb in rebalanceamentos)
            {
                //buscar custodia do cliente para obter preco medio
                var contaGrafica = await _context.ContasGraficas
                    .FirstOrDefaultAsync(cg => cg.ClienteId == clienteId && cg.Tipo == "FILHOTE");

                if (contaGrafica == null)
                {
                    continue;
                }

                var custodia = await _context.Custodias
                    .FirstOrDefaultAsync(c => c.ContaGraficaId == contaGrafica.Id && c.Ticker == reb.TickerVendido);

                if (custodia == null)
                {
                    continue;
                }

                //Lucro = Valor Venda - (Quantidade Vendida x Preco Medio)
                var custoAquisicao = reb.QuantidadeVendida * custodia.PrecoMedio;
                var lucro = reb.ValorVenda - custoAquisicao;
                lucroTotal += lucro;
            }

            //RN-061: se houver prejuizo, IR = 0
            if (lucroTotal <= 0)
            {
                return 0;
            }

            //RN-059: 20% sobre lucro liquido
            return Math.Round(lucroTotal * ALIQUOTA_IR_VENDA, 2);
        }
    }
}
