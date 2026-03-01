using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompraProgramada.Domain.Entities
{
    [Table("custodias")]
    public class Custodia
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Column("conta_grafica_id")]
        public long ContaGraficaId { get; set; }

        [Required]
        [Column("ticker")]
        [MaxLength(10)]
        public string Ticker { get; set; } = string.Empty;

        [Column("quantidade")]
        public int Quantidade { get; set; }

        [Column("preco_medio" , TypeName = "decimal(18,4)")]
        public decimal PrecoMedio { get; set; }

        [Column("data_ultima_atualizacao")]
        public DateTime DataUltimaAtualizacao { get; set; } = DateTime.UtcNow;

        //navegação
        public ContaGrafica? ContaGrafica { get; set; }
    }
}
