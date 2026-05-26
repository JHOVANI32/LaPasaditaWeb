using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaPasaditaWeb.Data;
using LaPasaditaWeb.Models;

namespace LaPasaditaWeb.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public class AdminConfiguracionApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminConfiguracionApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/AdminConfiguracionApi
        [HttpGet]
        public async Task<IActionResult> GetConfiguracion()
        {
            var config = await _context.ConfiguracionTienda.FirstOrDefaultAsync();
            if (config == null)
            {
                // Si no existe, creamos una por defecto
                config = new ConfiguracionTienda
                {
                    NombreTienda = "La Pasadita",
                    CostoEnvioBase = 15.00m
                };
                _context.ConfiguracionTienda.Add(config);
                await _context.SaveChangesAsync();
            }
            return Ok(config);
        }

        // PUT: api/AdminConfiguracionApi
        [HttpPut]
        public async Task<IActionResult> UpdateConfiguracion([FromBody] ConfiguracionTienda configUpdate)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var config = await _context.ConfiguracionTienda.FirstOrDefaultAsync();
            if (config == null)
            {
                _context.ConfiguracionTienda.Add(configUpdate);
            }
            else
            {
                config.NombreTienda = configUpdate.NombreTienda;
                config.TelefonoContacto = configUpdate.TelefonoContacto;
                config.EmailContacto = configUpdate.EmailContacto;
                config.DireccionFisica = configUpdate.DireccionFisica;
                config.HorarioAtencion = configUpdate.HorarioAtencion;
                config.CostoEnvioBase = configUpdate.CostoEnvioBase;
                
                // SMTP Config
                config.SmtpEmail = configUpdate.SmtpEmail;
                config.SmtpPassword = configUpdate.SmtpPassword;
                config.SmtpHost = configUpdate.SmtpHost;
                config.SmtpPort = configUpdate.SmtpPort;

                // Logo/Branding
                config.LogoUrl = configUpdate.LogoUrl;

                _context.ConfiguracionTienda.Update(config);
            }

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Configuración actualizada con éxito." });
        }

        // POST: api/AdminConfiguracionApi/upload-logo
        // Receives a file and stores it under wwwroot/uploads/logos, returns the public URL
        [HttpPost("upload-logo")]
        public async Task<IActionResult> UploadLogo([FromForm] IFormFile logoFile)
        {
            if (logoFile == null || logoFile.Length == 0)
                return BadRequest(new { mensaje = "No se recibió ningún archivo." });

            // Ensure safe file name
            var fileName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + Path.GetExtension(logoFile.FileName);
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "logos");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await logoFile.CopyToAsync(stream);
            }

            // Build URL relative to site root
            var url = $"/uploads/logos/{fileName}";
            return Ok(new { logoUrl = url });
        }
    }
}

