// Proyecto: La Pasadita - Programación Web III | Desarrollador: Jhovani Hernandez Pablo
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaPasaditaWeb.Models
{
    [Table("carrito_temporal")]
    public class CarritoTemporal
    {
        [Key]
        public int Id { get; set; }

        public int? UsuarioId { get; set; }
        [ForeignKey(nameof(UsuarioId))]
        public Usuario? Usuario { get; set; }

        public int? SesionInvitadoId { get; set; }
        [ForeignKey(nameof(SesionInvitadoId))]
        public SesionInvitado? SesionInvitado { get; set; }

        public int ProductoId { get; set; }
        [ForeignKey(nameof(ProductoId))]
        public Producto? Producto { get; set; }

        public int Cantidad { get; set; } = 1;

        public DateTime FechaAgregado { get; set; } = DateTime.UtcNow;
    }
}
