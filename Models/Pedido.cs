// Proyecto: La Pasadita - Programación Web III | Desarrollador: Jhovani Hernandez Pablo
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaPasaditaWeb.Models
{
    [Table("pedidos")]
    public class Pedido
    {
        [Key]
        public int Id { get; set; }

        public int? UsuarioId { get; set; }
        [ForeignKey(nameof(UsuarioId))]
        public Usuario? Usuario { get; set; }

        public DateTime FechaPedido { get; set; } = DateTime.UtcNow;

        [Required]
        [Column(TypeName = "numeric(18,2)")]
        public decimal Total { get; set; }

        [Required]
        [StringLength(50)]
        public string Estado { get; set; } = "Pendiente";

        public int? CuponId { get; set; }
        [ForeignKey(nameof(CuponId))]
        public Cupon? Cupon { get; set; }

        [Required]
        [StringLength(50)]
        public string MetodoPago { get; set; } = string.Empty;

        [Column(TypeName = "numeric(18,2)")]
        public decimal CostoEnvio { get; set; }

        [Required]
        [StringLength(500)]
        public string DireccionEnvio { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string NombreCliente { get; set; } = string.Empty;

        [StringLength(20)]
        public string TelefonoCliente { get; set; } = string.Empty;

        [StringLength(150)]
        public string EmailCliente { get; set; } = string.Empty;

        // Navegación
        public ICollection<DetallePedido> Detalles { get; set; } = new List<DetallePedido>();
        public ICollection<HistorialEstado> HistorialEstados { get; set; } = new List<HistorialEstado>();
    }
}
