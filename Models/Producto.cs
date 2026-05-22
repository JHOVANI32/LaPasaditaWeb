// Proyecto: La Pasadita - Programación Web III | Desarrollador: Jhovani Hernandez Pablo
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaPasaditaWeb.Models
{
    [Table("productos")]
    public class Producto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        [Required]
        [Column(TypeName = "numeric(18,2)")]
        public decimal Precio { get; set; }

        public int Stock { get; set; } = 0;

        [StringLength(300)]
        public string? ImagenUrl { get; set; }

        public int CategoriaId { get; set; }

        [ForeignKey(nameof(CategoriaId))]
        public Categoria? Categoria { get; set; }

        public bool Activo { get; set; } = true;

        // Navegación
        public ICollection<DetallePedido> Detalles { get; set; } = new List<DetallePedido>();
        public ICollection<CalificacionProducto> Calificaciones { get; set; } = new List<CalificacionProducto>();
        public ICollection<HistorialPrecio> HistorialPrecios { get; set; } = new List<HistorialPrecio>();
        public ICollection<Promocion> Promociones { get; set; } = new List<Promocion>();
    }
}
