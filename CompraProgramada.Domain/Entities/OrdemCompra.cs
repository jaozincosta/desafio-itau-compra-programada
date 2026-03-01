using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompraProgramada.Domain.Entities
{
    [Table("ordens_compra")]
    public class OrdemCompra
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Column("conta_master_id")]
        public long ContaMasterId { get; set; }

        [Required]
        [Column("ticker")]
        [MaxLength(10)]
        public string Ticker { get; set; } = string.Empty;

        [Column("quantidade")]
        public int Quantidade { get; set; }

        [Column("preco_unitario", TypeName = "decimal(18,4)")]
        public decimal PrecoUnitario { get; set; }

        [Required]
        [Column("tipo_mercado")]
        [MaxLength(15)]
        public string TipoMercado { get; set; } = string.Empty;

        [Column("data_execucao")]
        public DateTime DataExecucao { get; set; } = DateTime.UtcNow;


        //navegação
        public ContaGrafica? ContaGrafica { get; set; }
        public ICollection<Distribuicao> Distribuicoes { get; set; } = new List<Distribuicao>();
    }
}
    