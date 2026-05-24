using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaPasaditaWeb.Data;

namespace LaPasaditaWeb.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class CampanasApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CampanasApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/CampanasApi/activas
        [HttpGet("activas")]
        public async Task<IActionResult> GetCampanasActivas()
        {
            // Retorna las campañas que están activas y que no han alcanzado sus límites diarios ni de early bird.
            var campanasActivas = await _context.CampanasCupones
                .Include(c => c.PedidosPremiados)
                .Where(c => c.Activo)
                .ToListAsync();

            var campanasDisponibles = campanasActivas
                .Where(c => (c.LimiteDiario == 0 || c.CuponesGeneradosHoy < c.LimiteDiario) &&
                            (c.LimiteEarlyBird == 0 || c.PedidosPremiados.Count < c.LimiteEarlyBird))
                .Select(c => new
                {
                    c.Titulo,
                    c.MensajeBanner,
                    c.MontoMinimo,
                    c.TipoRecompensa,
                    c.ValorRecompensaFija,
                    c.ValoresSorpresa
                })
                .ToList();

            return Ok(campanasDisponibles);
        }
    }
}
