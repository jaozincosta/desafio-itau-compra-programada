using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompraProgramada.Domain.Entities
{
    [Table("clientes")]
    public class Cliente
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [Column("nome")]
        [MaxLength(200)]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [Column("cpf")]
        [MaxLength(11)]
        public string Cpf { get; set; } = string.Empty;

        [Required]
        [Column("email")]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Column("valor_mensal", TypeName = "decimal(18,2)")]
        public decimal ValorMensal { get; set; }

        [Column("ativo")]
        public bool Ativo { get; set; } = true;

        [Column("data_adesao")]
        public DateTime DataAdesao { get; set; } = DateTime.UtcNow;

        [Column("data_saida")]
        public DateTime? DataSaida { get; set; }

        //navegação
        public ContaGrafica? ContaGrafica { get; set; }
    }
}
