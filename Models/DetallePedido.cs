// Proyecto: La Pasadita - Programación Web III | Desarrollador: Jhovani Hernandez Pablo
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaPasaditaWeb.Models
{
    [Table("detalle_pedidos")]
    public class DetallePedido
    {
        [Key]
        public int Id { get; set; }

        public int PedidoId { get; set; }
        [ForeignKey(nameof(PedidoId))]
        public Pedido? Pedido { get; set; }

        public int ProductoId { get; set; }
        [ForeignKey(nameof(ProductoId))]
        public Producto? Producto { get; set; }

        public int Cantidad { get; set; }

        [Required]
        [Column(TypeName = "numeric(18,2)")]
        public decimal PrecioUnitario { get; set; }
    }
}
