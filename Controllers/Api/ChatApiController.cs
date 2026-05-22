using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LaPasaditaWeb.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatApiController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public ChatApiController(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        [HttpPost]
        public async Task<IActionResult> EnviarMensaje([FromBody] ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.Mensaje))
            {
                return BadRequest(new { error = "El mensaje no puede estar vacío." });
            }

            string? apiKey = _configuration["GeminiAI:ApiKey"];
            
            if (string.IsNullOrEmpty(apiKey))
            {
                return Ok(new { 
                    respuesta = "¡Hola! Soy el asistente virtual (modo simulación). Tu mensaje fue: '" + request.Mensaje + "'."
                });
            }

            try
            {
                // Formato de payload para Google Gemini 1.5 Flash
                var payload = new
                {
                    system_instruction = new 
                    {
                        parts = new[] 
                        {
                            new { text = "Eres el asistente virtual amigable de una pequeña tienda de abarrotes llamada 'La Pasadita'. Tu objetivo es ayudar a los clientes con dudas sobre los productos, horarios (Lunes a Sábado de 8am a 8pm), y cómo hacer sus compras en línea. Sé conciso y muy amable." }
                        }
                    },
                    contents = new[]
                    {
                        new 
                        { 
                            parts = new[] 
                            {
                                new { text = request.Mensaje }
                            }
                        }
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";
                
                var response = await _httpClient.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var jsonDoc = JsonDocument.Parse(responseString);
                    
                    // Extraer la respuesta: candidates[0].content.parts[0].text
                    var aiMessage = jsonDoc.RootElement
                                           .GetProperty("candidates")[0]
                                           .GetProperty("content")
                                           .GetProperty("parts")[0]
                                           .GetProperty("text")
                                           .GetString();

                    return Ok(new { respuesta = aiMessage });
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { error = "Error en la API de Google Gemini.", detalles = responseString });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error interno del servidor.", detalles = ex.Message });
            }
        }
    }

    public class ChatRequest
    {
        public string Mensaje { get; set; } = string.Empty;
    }
}
