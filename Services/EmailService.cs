using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using LaPasaditaWeb.Data;

namespace LaPasaditaWeb.Services
{
    public class EmailService : IEmailService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IServiceProvider serviceProvider, ILogger<EmailService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task EnviarCorreoAsync(string destinatario, string asunto, string mensajeHtml)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var config = await context.ConfiguracionTienda.FirstOrDefaultAsync();
            
            if (config == null || string.IsNullOrEmpty(config.SmtpEmail) || string.IsNullOrEmpty(config.SmtpPassword) || string.IsNullOrEmpty(config.SmtpHost) || config.SmtpPort == 0)
            {
                _logger.LogWarning("Configuración SMTP incompleta. No se pudo enviar el correo a: {destinatario}", destinatario);
                return;
            }

            try
            {
                var smtpClient = new SmtpClient(config.SmtpHost)
                {
                    Port = config.SmtpPort,
                    Credentials = new NetworkCredential(config.SmtpEmail, config.SmtpPassword),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(config.SmtpEmail, config.NombreTienda),
                    Subject = asunto,
                    Body = mensajeHtml,
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(destinatario);

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Correo enviado exitosamente a {destinatario}", destinatario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo a {destinatario}", destinatario);
            }
        }
    }
}
