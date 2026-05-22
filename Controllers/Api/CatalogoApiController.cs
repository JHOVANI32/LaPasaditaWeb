using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaPasaditaWeb.Data;
using System.Linq;
using System.Threading.Tasks;

namespace LaPasaditaWeb.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class CatalogoApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CatalogoApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/CatalogoApi
        [HttpGet]
        public async Task<IActionResult> GetCatalogo()
        {
            var categorias = await _context.Categorias
                .Where(c => c.Activo)
                .Select(c => new { c.Id, c.Nombre, c.Descripcion })
                .ToListAsync();

            var productos = await _context.Productos
                .Where(p => p.Activo && p.Stock > 0)
                .Select(p => new {
                    p.Id,
                    p.Nombre,
                    p.Descripcion,
                    p.Precio,
                    p.Stock,
                    p.ImagenUrl,
                    p.CategoriaId
                })
                .ToListAsync();

            var configuracion = await _context.ConfiguracionTienda
                .Select(t => new { t.NombreTienda, t.HorarioAtencion, t.TelefonoContacto, t.DireccionFisica })
                .FirstOrDefaultAsync();

            return Ok(new { categorias, productos, configuracion });
        }

        // GET: api/CatalogoApi/buscar
        [HttpGet("buscar")]
        public async Task<IActionResult> Buscar([FromQuery] string? q, [FromQuery] int? categoriaId)
        {
            var query = _context.Productos.Where(p => p.Activo && p.Stock > 0);

            if (!string.IsNullOrEmpty(q))
            {
                var queryLower = q.ToLower();
                query = query.Where(p => p.Nombre.ToLower().Contains(queryLower) || 
                                         (p.Descripcion != null && p.Descripcion.ToLower().Contains(queryLower)));
            }

            if (categoriaId.HasValue && categoriaId > 0)
            {
                query = query.Where(p => p.CategoriaId == categoriaId.Value);
            }

            var productos = await query
                .Select(p => new {
                    p.Id,
                    p.Nombre,
                    p.Descripcion,
                    p.Precio,
                    p.Stock,
                    p.ImagenUrl,
                    p.CategoriaId
                })
                .ToListAsync();

            return Ok(productos);
        }
    }
}
