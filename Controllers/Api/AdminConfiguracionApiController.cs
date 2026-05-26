using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaPasaditaWeb.Data;
using LaPasaditaWeb.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
namespace LaPasaditaWeb.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public class AdminConfiguracionApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminConfiguracionApiController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
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
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "logos");
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

        // POST: api/AdminConfiguracionApi/test-smtp
        [HttpPost("test-smtp")]
        public async Task<IActionResult> TestSmtp([FromBody] SmtpTestRequest request)
        {
            if (string.IsNullOrEmpty(request.Destinatario))
            {
                return BadRequest(new { mensaje = "El correo del destinatario es requerido." });
            }

            if (string.IsNullOrEmpty(request.SmtpEmail) || string.IsNullOrEmpty(request.SmtpPassword) || string.IsNullOrEmpty(request.SmtpHost) || request.SmtpPort == 0)
            {
                return BadRequest(new { mensaje = "Por favor, completa todos los campos de SMTP para realizar la prueba." });
            }

            try
            {
                using var smtpClient = new System.Net.Mail.SmtpClient(request.SmtpHost)
                {
                    Port = request.SmtpPort,
                    Credentials = new System.Net.NetworkCredential(request.SmtpEmail, request.SmtpPassword),
                    EnableSsl = true,
                };

                using var mailMessage = new System.Net.Mail.MailMessage
                {
                    From = new System.Net.Mail.MailAddress(request.SmtpEmail, request.NombreTienda),
                    Subject = "Prueba de Configuración SMTP - La Pasadita",
                    Body = $@"
                        <div style='font-family: Arial, sans-serif; padding: 20px; max-width: 600px; border: 1px solid #ddd; border-radius: 10px; margin: 0 auto; background-color: #ffffff;'>
                            <div style='text-align: center; margin-bottom: 20px;'>
                                <span style='font-size: 2.5em;'>✉️</span>
                            </div>
                            <h2 style='color: #28a745; text-align: center; margin-top: 0;'>¡Conexión Exitosa!</h2>
                            <p style='font-size: 1.1em; color: #333; text-align: center;'>Este es un correo de prueba enviado desde tu tienda <strong>{request.NombreTienda}</strong>.</p>
                            <p style='color: #555;'>Tu configuración de correo SMTP funciona perfectamente y está lista para ser utilizada para enviar recibos y notificaciones automáticas a tus clientes.</p>
                            <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; border-left: 4px solid #28a745; margin: 20px 0;'>
                                <strong style='color: #333; display: block; margin-bottom: 5px;'>Detalles de la conexión:</strong>
                                <span style='font-size: 0.9em; color: #555;'>
                                    <strong>Servidor SMTP:</strong> {request.SmtpHost}<br/>
                                    <strong>Puerto:</strong> {request.SmtpPort}<br/>
                                    <strong>Remitente:</strong> {request.SmtpEmail}
                                </span>
                            </div>
                            <p style='font-size: 0.85em; color: #777; text-align: center;'>Puedes cerrar esta prueba de forma segura y guardar tu configuración.</p>
                        </div>",
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(request.Destinatario);

                await smtpClient.SendMailAsync(mailMessage);
                return Ok(new { mensaje = "¡Correo de prueba enviado con éxito! Revisa tu bandeja de entrada (y la carpeta de Spam/Promociones)." });
            }
            catch (System.Exception ex)
            {
                var errorMsg = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMsg += " -> " + ex.InnerException.Message;
                }
                return BadRequest(new { mensaje = $"Error de conexión: {errorMsg}" });
            }
        }
    }

    public class SmtpTestRequest
    {
        public string Destinatario { get; set; } = string.Empty;
        public string SmtpEmail { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string NombreTienda { get; set; } = "La Pasadita";
    }
}

