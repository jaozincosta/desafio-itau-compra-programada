using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompraProgramada.Infrastructure.Data
{
    public class AppDbContext : DbContext, IAppDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        
        public DbSet<Cliente> Clientes => Set<Cliente>();
        public DbSet<ContaGrafica> ContasGraficas => Set<ContaGrafica>();
        public DbSet<Custodia> Custodias => Set<Custodia>();
        public DbSet<CestaRecomendacao> CestasRecomendacao => Set<CestaRecomendacao>();
        public DbSet<ItemCesta> ItensCesta => Set<ItemCesta>();
        public DbSet<OrdemCompra> OrdensCompra => Set<OrdemCompra>();
        public DbSet<Distribuicao> Distribuicoes => Set<Distribuicao>();
        public DbSet<EventoIR> EventosIR => Set<EventoIR>();
        public DbSet<Cotacao> Cotacoes => Set<Cotacao>();
        public DbSet<Rebalanceamento> Rebalanceamentos => Set<Rebalanceamento>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Cliente -> ContaGrafica (1:1)
            modelBuilder.Entity<Cliente>()
                .HasOne(c => c.ContaGrafica)
                .WithOne(cg => cg.Cliente)
                .HasForeignKey<ContaGrafica>(cg => cg.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cliente>()
                .HasIndex(c => c.Cpf)
                .IsUnique();


            //ContaGrafica -> Custodia (1:N)
            modelBuilder.Entity<Custodia>()
                .HasOne(c => c.ContaGrafica)
                .WithMany(cg => cg.Custodias)
                .HasForeignKey(c => c.ContaGraficaId)
                .OnDelete(DeleteBehavior.Restrict);


            //CestaRecomendacao -> ItensCesta (1:N)
            modelBuilder.Entity<ItemCesta>()
                .HasOne(i => i.Cesta)
                .WithMany(c => c.Itens)
                .HasForeignKey(i => i.CestaId)
                .OnDelete(DeleteBehavior.Cascade);


            //OrdemCompra -> Distribuicao (1:N)
            modelBuilder.Entity<Distribuicao>()
                .HasOne(d => d.OrdemCompra)
                .WithMany(o => o.Distribuicoes)
                .HasForeignKey(d => d.OrdemCompraId)
                .OnDelete(DeleteBehavior.Restrict);


            //OrdemCompra -> ContaMaster
            modelBuilder.Entity<OrdemCompra>()
                .HasOne(o => o.ContaGrafica)
                .WithMany()
                .HasForeignKey(o => o.ContaMasterId)
                .OnDelete(DeleteBehavior.Restrict);


            //EventoIR -> Cliente
            modelBuilder.Entity<EventoIR>()
                .HasOne(e => e.Cliente)
                .WithMany()
                .HasForeignKey(e => e.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);


            //Rebalanceamento -> Cliente
            modelBuilder.Entity<Rebalanceamento>()
                .HasOne(r => r.Cliente)
                .WithMany()
                .HasForeignKey(r => r.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);


            //Indice unico para cotação (ticker + data)
            modelBuilder.Entity<Cotacao>()
                .HasIndex(c => new { c.Ticker, c.DataPregao })
                .IsUnique();

            //Indice unico para custodia (conta + ticker)
            modelBuilder.Entity<Custodia>()
                .HasIndex(c => new { c.ContaGraficaId, c.Ticker })
                .IsUnique();

            //Conta Master (a corretora precisa dessa conta)
            modelBuilder.Entity<ContaGrafica>()
                .HasData(new ContaGrafica
                {
                    Id = 1,
                    ClienteId = null, // Conta Master não tem cliente associado
                    NumeroConta = "MST-000001",
                    Tipo = "MASTER",
                    DataCriacao = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                });
        }
    }
}
