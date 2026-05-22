// Proyecto: La Pasadita - Programación Web III | Desarrollador: Jhovani Hernandez Pablo
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaPasaditaWeb.Models
{
    [Table("configuracion_tienda")]
    public class ConfiguracionTienda
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string NombreTienda { get; set; } = string.Empty;

        [StringLength(20)]
        public string? TelefonoContacto { get; set; }

        [StringLength(100)]
        public string? EmailContacto { get; set; }

        [StringLength(200)]
        public string? DireccionFisica { get; set; }

        [StringLength(100)]
        public string? HorarioAtencion { get; set; }

        [Required]
        [Column(TypeName = "numeric(18,2)")]
        public decimal CostoEnvioBase { get; set; }
    }
}
