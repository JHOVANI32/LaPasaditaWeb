// Proyecto: La Pasadita - Programación Web III | Desarrollador: Jhovani Hernandez Pablo
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaPasaditaWeb.Models
{
    [Table("calificaciones_productos")]
    public class CalificacionProducto
    {
        [Key]
        public int Id { get; set; }

        public int ProductoId { get; set; }
        [ForeignKey(nameof(ProductoId))]
        public Producto? Producto { get; set; }

        public int UsuarioId { get; set; }
        [ForeignKey(nameof(UsuarioId))]
        public Usuario? Usuario { get; set; }

        [Required]
        [Range(1, 5)]
        public int Puntuacion { get; set; }

        [StringLength(500)]
        public string? Comentario { get; set; }

        public DateTime FechaCalificacion { get; set; } = DateTime.UtcNow;
    }
}
