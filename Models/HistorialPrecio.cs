// Proyecto: La Pasadita - Programación Web III | Desarrollador: Jhovani Hernandez Pablo
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaPasaditaWeb.Models
{
    [Table("historial_precios")]
    public class HistorialPrecio
    {
        [Key]
        public int Id { get; set; }

        public int? ProductoId { get; set; }
        [ForeignKey(nameof(ProductoId))]
        public Producto? Producto { get; set; }

        [Required]
        [Column(TypeName = "numeric(18,2)")]
        public decimal PrecioAnterior { get; set; }

        [Required]
        [Column(TypeName = "numeric(18,2)")]
        public decimal PrecioNuevo { get; set; }

        public DateTime FechaCambio { get; set; } = DateTime.UtcNow;
    }
}
