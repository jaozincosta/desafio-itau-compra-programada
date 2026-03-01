using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Application.Interfaces
{
    public interface IAppDbContext
    {
        DbSet<Cliente> Clientes { get; }
        DbSet<ContaGrafica> ContasGraficas { get; }
        DbSet<Custodia> Custodias { get; }
        DbSet<CestaRecomendacao> CestasRecomendacao { get; }
        DbSet<ItemCesta> ItensCesta { get; }
        DbSet<OrdemCompra> OrdensCompra { get; }
        DbSet<Distribuicao> Distribuicoes { get; }
        DbSet<EventoIR> EventosIR { get; }
        DbSet<Cotacao> Cotacoes { get; }
        DbSet<Rebalanceamento> Rebalanceamentos { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}