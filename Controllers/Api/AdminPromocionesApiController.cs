using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaPasaditaWeb.Data;
using LaPasaditaWeb.Models;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace LaPasaditaWeb.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminPromocionesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminPromocionesApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/AdminPromocionesApi
        [HttpGet]
        public async Task<IActionResult> GetPromociones()
        {
            var promociones = await _context.Promociones
                .Include(p => p.Producto)
                .OrderByDescending(p => p.Id)
                .Select(p => new
                {
                    p.Id,
                    p.Titulo,
                    p.Descripcion,
                    p.ProductoId,
                    NombreProducto = p.Producto != null ? p.Producto.Nombre : "Todos los productos",
                    p.DescuentoPorcentaje,
                    p.FechaInicio,
                    p.FechaFin,
                    p.Activo,
                    Estado = p.Activo && p.FechaInicio <= DateTime.Now && p.FechaFin >= DateTime.Now ? "Activa" :
                             p.Activo && p.FechaInicio > DateTime.Now ? "Programada" :
                             !p.Activo ? "Inactiva" : "Expirada"
                })
                .ToListAsync();

            return Ok(promociones);
        }

        // POST: api/AdminPromocionesApi
        [HttpPost]
        public async Task<IActionResult> CreatePromocion([FromBody] Promocion promocion)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validar fechas
            if (promocion.FechaFin <= promocion.FechaInicio)
                return BadRequest(new { message = "La fecha de fin debe ser mayor a la fecha de inicio." });

            _context.Promociones.Add(promocion);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPromociones), new { id = promocion.Id }, promocion);
        }

        // DELETE: api/AdminPromocionesApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePromocion(int id)
        {
            var promocion = await _context.Promociones.FindAsync(id);
            if (promocion == null)
                return NotFound();

            // En lugar de eliminar físicamente, podríamos desactivar o eliminar. 
            // Como es una promoción, borrarla está bien si no afecta historial estricto, 
            // o simplemente la marcamos inactiva. Vamos a eliminarla para simplificar la UI.
            _context.Promociones.Remove(promocion);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Promoción eliminada correctamente." });
        }

        // PUT: api/AdminPromocionesApi/toggle/5
        [HttpPut("toggle/{id}")]
        public async Task<IActionResult> ToggleActivo(int id)
        {
            var promocion = await _context.Promociones.FindAsync(id);
            if (promocion == null)
                return NotFound();

            promocion.Activo = !promocion.Activo;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Estado actualizado", activo = promocion.Activo });
        }
    }
}
