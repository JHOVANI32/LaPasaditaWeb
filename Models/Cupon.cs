// Proyecto: La Pasadita - Programación Web III | Desarrollador: Jhovani Hernandez Pablo
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaPasaditaWeb.Models
{
    [Table("cupones")]
    public class Cupon
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string TipoDescuento { get; set; } = "Porcentaje";

        [Required]
        [Column(TypeName = "numeric(18,2)")]
        public decimal Valor { get; set; }

        public DateTime FechaExpiracion { get; set; }

        public int LimiteUso { get; set; } = 1;
        public int UsosActuales { get; set; } = 0;
        public bool Activo { get; set; } = true;

        // Navegación
        public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
    }
}
