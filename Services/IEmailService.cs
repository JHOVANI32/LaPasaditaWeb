using System.Threading.Tasks;

namespace LaPasaditaWeb.Services
{
    public interface IEmailService
    {
        Task EnviarCorreoAsync(string destinatario, string asunto, string mensajeHtml);
    }
}
