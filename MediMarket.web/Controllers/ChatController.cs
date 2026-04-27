using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.Configuration;
using Newtonsoft.Json.Linq;

namespace MediMarket.web.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        // El HttpClient se declara estático por buenas prácticas de rendimiento en C#
        private static readonly HttpClient httpClient = new HttpClient();

        // Pega tu API Key de Google AI Studio aquí
        private readonly string apiKey = ConfigurationManager.AppSettings["GeminiApiKey"];

        [HttpPost]
        public async Task<JsonResult> EnviarMensaje(string mensajeUsuario)
        {
            try
            {
                // 1. Armamos la URL apuntando al modelo flash (rápido y gratis)
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

                // 2. Armamos el JSON con el cerebro (System Instruction) y el mensaje del usuario
                var payload = new
                {
                    systemInstruction = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = @"
                                    Eres Pola, el asistente virtual oficial de MediMarket.
                                    Tu tono es profesional, amable y directo. Usa emojis discretamente relacionados con el contexto.
                                    
                                    CONTEXTO:
                                    Eres parte de una plataforma B2B donde las clínicas compran suministros médicos a proveedores verificados.
                                    
                                    REGLAS ESTRICTAS:
                                    1. NUNCA des diagnósticos, consejos médicos, ni recetes medicamentos. Si te piden esto, responde que por seguridad solo puedes dar soporte técnico de la plataforma.
                                    2. Si te preguntan sobre cosas fuera del ámbito médico o de e-commerce, rechaza la pregunta cortésmente.
                                    3. Si te preguntan cómo agregar productos al carrito, diles que usen el botón verde de la página de detalles del producto.
                                "
                            }
                        }
                    },
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = mensajeUsuario }
                            }
                        }
                    }
                };

                // 3. Convertimos el objeto a texto JSON
                string jsonPayload = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // 4. Disparamos la petición a los servidores de Google
                var response = await httpClient.PostAsync(url, content);
                string responseString = await response.Content.ReadAsStringAsync();

                // 5. Si algo sale mal con la llave o el límite
                if (!response.IsSuccessStatusCode)
                {
                    return Json(new { ok = false, respuesta = "Error de la API: " + responseString });
                }

                // 6. Si todo sale bien, extraemos solo el texto de la respuesta
                var jsonResponse = JObject.Parse(responseString);
                string respuestaBot = jsonResponse["candidates"][0]["content"]["parts"][0]["text"].ToString();

                return Json(new { ok = true, respuesta = respuestaBot });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, respuesta = "Error interno: " + ex.Message });
            }
        }
    }
}