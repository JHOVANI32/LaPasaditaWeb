// Proyecto: La Pasadita - Programación Web III | Desarrollador: Jhovani Hernandez Pablo
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaPasaditaWeb.Models
{
    [Table("sesiones_invitados")]
    public class SesionInvitado
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string TokenSesion { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime FechaUltimaActividad { get; set; } = DateTime.UtcNow;

        // Navegación
        public ICollection<CarritoTemporal> CarritoItems { get; set; } = new List<CarritoTemporal>();
    }
}
