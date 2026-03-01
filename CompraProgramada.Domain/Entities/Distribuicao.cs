using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompraProgramada.Domain.Entities
{
    [Table("distribuicoes")]
    public class Distribuicao
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Column("ordem_compra_id")]
        public long OrdemCompraId { get; set; }

        [Column("custodia_filhote_id")]
        public long CustodiaFilhoteId { get; set; }

        [Required]
        [Column("ticker")]
        [MaxLength(10)]
        public string Ticker { get; set; } = string.Empty;

        [Column("quantidade")]
        public int Quantidade { get; set; }

        [Column("preco_unitario", TypeName = "decimal(18,4)")]
        public decimal PrecoUnitario { get; set; }

        [Column("data_distribuicao")]
        public DateTime DataDistribuicao { get; set; } = DateTime.UtcNow;

        //navegação
        public OrdemCompra? OrdemCompra { get; set; }

    }
}
