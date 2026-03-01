using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompraProgramada.Domain.Entities
{
    [Table("itens_cesta")]
    public class ItemCesta
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Column("cesta_id")]
        public long CestaId { get; set; }

        [Required]
        [Column("ticker")]
        [MaxLength(10)]
        public string Ticker { get; set; } = string.Empty;

        [Column("percentual", TypeName = "decimal(5,2)")]
        public decimal Percentual { get; set; }


        //navegação
        public CestaRecomendacao? Cesta { get; set; }  
    }
}
