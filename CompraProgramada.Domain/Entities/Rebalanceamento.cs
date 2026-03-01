using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompraProgramada.Domain.Entities
{
    [Table("rebalanceamentos")]
    public class Rebalanceamento
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Column("cliente_id")]
        public long ClienteId { get; set; }

        [Required]
        [Column("tipo")]
        [MaxLength(20)]
        public string Tipo { get; set; } = string.Empty;

        [Column("ticker_vendido")]
        [MaxLength(10)]
        public string? TickerVendido { get; set; }

        [Column("ticker_comprado")]
        [MaxLength(10)]
        public string? TickerComprado { get; set; }

        [Column("valor_venda", TypeName = "decimal(18,2)")]
        public decimal ValorVenda { get; set; }

        [Column("quantidade_vendida")]
        public int QuantidadeVendida { get; set; }

        [Column("quantidade_comprada")]
        public int QuantidadeComprada { get; set; }

        [Column("data_rebalanceamento")]
        public DateTime DataRebalanceamento { get; set; } = DateTime.UtcNow;


        //navegação
        public Cliente? Cliente { get; set; }
    }
}
