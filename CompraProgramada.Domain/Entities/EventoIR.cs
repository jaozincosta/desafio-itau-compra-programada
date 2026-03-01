using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompraProgramada.Domain.Entities
{
    [Table("eventos_ir")]
    public class EventoIR
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Column("cliente_id")]
        public long ClienteId { get; set; }

        [Required]
        [Column("tipo")]
        [MaxLength(15)]
        public string Tipo { get; set; } = string.Empty;

        [Column("valor_base", TypeName = "decimal(18,2)")]
        public decimal ValorBase { get; set; }

        [Column("valor_ir", TypeName = "decimal(18,2)")]
        public decimal ValorIr { get; set; }

        [Column("publicado_kafka")]
        public bool PublicadoKafka { get; set; }

        [Column("data_evento")]
        public DateTime DataEvento { get; set; } = DateTime.UtcNow;


         //navegação
         public Cliente? Cliente { get; set; }
    }
}
