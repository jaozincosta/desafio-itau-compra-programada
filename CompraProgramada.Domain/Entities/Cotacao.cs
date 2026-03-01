using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompraProgramada.Domain.Entities
{
    [Table("cotacoes")]
    public class Cotacao
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Column("data_pregao", TypeName = "date")]
        public DateTime DataPregao { get; set; }    

        [Required]
        [Column("ticker")]
        [MaxLength(10)]
        public string Ticker { get; set; } = string.Empty;

        [Column("preco_abertura", TypeName = "decimal(18,4)")]
        public decimal PrecoAbertura { get; set; }

        [Column("preco_fechamento", TypeName = "decimal(18,4)")]
        public decimal PrecoFechamento { get; set; }

        [Column("preco_maximo", TypeName = "decimal(18,4)")]
        public decimal PrecoMaximo { get; set; }

        [Column("preco_minimo", TypeName = "decimal(18,4)")]
        public decimal PrecoMinimo { get; set; }
    }
}
