// Proyecto: La Pasadita - Programación Web III | Desarrollador: Jhovani Hernandez Pablo
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaPasaditaWeb.Models
{
    [Table("direcciones")]
    public class Direccion
    {
        [Key]
        public int Id { get; set; }

        public int UsuarioId { get; set; }
        [ForeignKey(nameof(UsuarioId))]
        public Usuario? Usuario { get; set; }

        [Required]
        [StringLength(200)]
        public string Calle { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Colonia { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Ciudad { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Estado { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string CodigoPostal { get; set; } = string.Empty;

        public bool EsPrincipal { get; set; } = false;
    }
}
