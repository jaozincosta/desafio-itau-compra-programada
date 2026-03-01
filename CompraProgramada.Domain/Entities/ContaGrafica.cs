using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompraProgramada.Domain.Entities
{
    [Table("contas_graficas")]
    public class ContaGrafica
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Column("cliente_id")]
        public long? ClienteId { get; set; }

        [Required]
        [Column("numero_conta")]
        [MaxLength(20)]
        public string NumeroConta { get; set; } = string.Empty;

        [Required]
        [Column("tipo")]
        [MaxLength(10)]
        public string Tipo { get; set; } = "FILHOTE";

        [Column("data_criacao")]
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        //nvavegação
        public Cliente? Cliente { get; set; }
        public ICollection<Custodia> Custodias { get; set; } = new List<Custodia>();
    }
}
