// Proyecto: La Pasadita - Programación Web III | Desarrollador: Jhovani Hernandez Pablo
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaPasaditaWeb.Models
{
    [Table("campanas_cupones")]
    public class CampanaCupon
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        public bool Activo { get; set; } = true;

        [Required]
        [Column(TypeName = "numeric(18,2)")]
        public decimal MontoMinimo { get; set; }

        [Required]
        [StringLength(20)]
        public string TipoRecompensa { get; set; } = "Fijo"; // "Fijo", "Sorpresa"

        [Column(TypeName = "numeric(18,2)")]
        public decimal? ValorRecompensaFija { get; set; }

        [StringLength(200)]
        public string? ValoresSorpresa { get; set; } // Ejemplo: "10,15,20,50" separados por comas

        public int LimiteDiario { get; set; } = 0; // 0 significa sin límite

        public int LimiteEarlyBird { get; set; } = 0; // 0 significa sin límite

        public int CuponesGeneradosHoy { get; set; } = 0; // Se reinicia cada día

        public DateTime FechaUltimoReinicio { get; set; } = DateTime.UtcNow;

        [StringLength(255)]
        public string? MensajeBanner { get; set; } // Ejemplo: "¡Oferta del día! Compras mayores a $500 se llevan un cupón sorpresa"
        
        // Navegación
        public ICollection<Pedido> PedidosPremiados { get; set; } = new List<Pedido>();
    }
}
