using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompraProgramada.Domain.Entities
{
    [Table("cestas_recomendacao")]
    public class CestaRecomendacao
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [Column("nome")]
        [MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Column("ativa")]
        public bool Ativa { get; set; } = true;

        [Column("data_criacao")]
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        [Column("data_desativacao")]
        public DateTime? DataDesativacao { get; set; }


        //navegação
        public ICollection<ItemCesta> Itens { get; set; } = new List<ItemCesta>();

    }
}
