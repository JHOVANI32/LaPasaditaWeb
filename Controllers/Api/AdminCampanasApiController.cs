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
    public class AdminCampanasApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminCampanasApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/AdminCampanasApi
        [HttpGet]
        public async Task<IActionResult> GetCampanas()
        {
            // Reiniciar contadores diarios si ha pasado un día
            var campanas = await _context.CampanasCupones.OrderByDescending(c => c.Id).ToListAsync();
            var ahora = DateTime.UtcNow;
            var huboCambios = false;

            foreach (var campana in campanas)
            {
                if (campana.FechaUltimoReinicio.Date < ahora.Date)
                {
                    campana.CuponesGeneradosHoy = 0;
                    campana.FechaUltimoReinicio = ahora;
                    huboCambios = true;
                }
            }

            if (huboCambios)
            {
                await _context.SaveChangesAsync();
            }

            return Ok(campanas);
        }

        // POST: api/AdminCampanasApi
        [HttpPost]
        public async Task<IActionResult> CreateCampana([FromBody] CampanaCupon campana)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            campana.FechaUltimoReinicio = DateTime.UtcNow;
            campana.CuponesGeneradosHoy = 0;

            _context.CampanasCupones.Add(campana);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCampanas), new { id = campana.Id }, campana);
        }

        // PUT: api/AdminCampanasApi/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCampana(int id, [FromBody] CampanaCupon campanaActualizada)
        {
            if (id != campanaActualizada.Id)
                return BadRequest(new { message = "ID mismatch" });

            var campana = await _context.CampanasCupones.FindAsync(id);
            if (campana == null)
                return NotFound();

            campana.Titulo = campanaActualizada.Titulo;
            campana.Activo = campanaActualizada.Activo;
            campana.MontoMinimo = campanaActualizada.MontoMinimo;
            campana.TipoRecompensa = campanaActualizada.TipoRecompensa;
            campana.ValorRecompensaFija = campanaActualizada.ValorRecompensaFija;
            campana.ValoresSorpresa = campanaActualizada.ValoresSorpresa;
            campana.LimiteDiario = campanaActualizada.LimiteDiario;
            campana.LimiteEarlyBird = campanaActualizada.LimiteEarlyBird;
            campana.MensajeBanner = campanaActualizada.MensajeBanner;

            await _context.SaveChangesAsync();
            return Ok(campana);
        }

        // DELETE: api/AdminCampanasApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCampana(int id)
        {
            var campana = await _context.CampanasCupones.FindAsync(id);
            if (campana == null)
                return NotFound();

            _context.CampanasCupones.Remove(campana);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Campaña eliminada correctamente." });
        }

        // PUT: api/AdminCampanasApi/toggle/5
        [HttpPut("toggle/{id}")]
        public async Task<IActionResult> ToggleActivo(int id)
        {
            var campana = await _context.CampanasCupones.FindAsync(id);
            if (campana == null)
                return NotFound();

            campana.Activo = !campana.Activo;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Estado actualizado", activo = campana.Activo });
        }
    }
}
