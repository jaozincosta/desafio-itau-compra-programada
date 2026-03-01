using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Application.Services
{
    public class ContaMasterService : IContaMasterService
    {
        private readonly IAppDbContext _context;

        public ContaMasterService(IAppDbContext context)
        {
            _context = context;
        }

        //Consultar custodia da conta master (residuos)
        public async Task<CustodiaMasterResponse> ConsultarCustodiaAsync()
        {
            var contaMaster = await _context.ContasGraficas
                .FirstOrDefaultAsync(cg => cg.Tipo == "MASTER");

            if (contaMaster == null)
                throw new KeyNotFoundException("CONTA_MASTER_NAO_ENCONTRADA");

            var custodias = await _context.Custodias
                .Where(c => c.ContaGraficaId == contaMaster.Id)
                .ToListAsync();

            var itens = custodias.Select(c => new CustodiaMasterItemDto(
                c.Ticker, c.Quantidade, c.PrecoMedio, null, "RESIDUO"
            )).ToList();

            var valorTotal = custodias.Sum(c => c.Quantidade * c.PrecoMedio);

            return new CustodiaMasterResponse(
                new ContaMasterDto(contaMaster.Id, contaMaster.NumeroConta, contaMaster.Tipo),
                itens, valorTotal);
        }
    }
}