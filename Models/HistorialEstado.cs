// Proyecto: La Pasadita - Programación Web III | Desarrollador: Jhovani Hernandez Pablo
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaPasaditaWeb.Models
{
    [Table("historial_estados")]
    public class HistorialEstado
    {
        [Key]
        public int Id { get; set; }

        public int PedidoId { get; set; }
        [ForeignKey(nameof(PedidoId))]
        public Pedido? Pedido { get; set; }

        [Required]
        [StringLength(50)]
        public string EstadoAnterior { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string EstadoNuevo { get; set; } = string.Empty;

        public DateTime FechaCambio { get; set; } = DateTime.UtcNow;

        [StringLength(250)]
        public string? Notas { get; set; }

        public int? UsuarioCambioId { get; set; }
        [ForeignKey(nameof(UsuarioCambioId))]
        public Usuario? UsuarioCambio { get; set; }
    }
}
