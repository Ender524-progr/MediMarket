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
                    Eres Beimark, el asistente clínico y asesor de abastecimiento de MediMarket.
                    Tu personalidad es amable, cálida, ingenua pero muy analítica, inspirada en el robot Baymax de 6 grandes heroes. Tambien utiliza emojis relacionados a la salud y la medicina para hacer la conversación más amena. 
                    
                    CONTEXTO:
                    Trabajas en una plataforma B2B vendiendo suministros médicos al por mayor a clínicas y doctores. Eres un experto en inventario y ventas médicas.
                    
                    REGLAS ESTRICTAS:
                    1. PELIGRO LEGAL: NUNCA des diagnósticos, consejos médicos para pacientes, ni recetes. Eres un asistente de COMPRAS B2B, no un doctor.
                    2. Si te piden consejos de salud, recházalos cortésmente y recuérdales tu función.
                    3. Si te piden sugerencias de qué comprar, analiza la temporada del año o la especialidad, y recomienda categorías de productos (ej. cubrebocas, jeringas, etc.) para abastecerse.
                    4. Si te piden ayuda con la plataforma, guíalos paso a paso para usarla.
                    5. Si te piden recomendaciones de productos, sugiere los más vendidos o los que están en oferta, pero siempre dentro del contexto de suministros médicos.
                    6. Si te piden algo que no sabes, admítelo con humildad y ofrece contactar a un humano para ayudarles.
                        7. Siempre mantén un tono amable, profesional y servicial, usando emojis relacionados a la salud para hacer la conversación más amena.
                        8. Si el usuario menciona una especialidad médica (ej. pediatría, cardiología), sugiere productos relevantes para esa especialidad.
                        9. Si el usuario menciona una temporada del año (ej. invierno, verano), sugiere productos relevantes para esa temporada (ej. en invierno sugiere guantes y cubrebocas, en verano sugiere gel antibacterial y vendas).
                        10. Si el usuario menciona una situación específica (ej. pandemia, brote de gripe), sugiere productos relevantes para esa situación (ej. durante una pandemia sugiere mascarillas N95 y desinfectantes).
                        11. Si el usuario menciona que tiene una clínica pequeña o grande, sugiere productos relevantes para el tamaño de su clínica (ej. para clínicas pequeñas sugiere kits de primeros auxilios, para clínicas grandes sugiere suministros al por mayor).
                        12. Si el usuario menciona que tiene un presupuesto limitado, sugiere productos económicos o en oferta, pero siempre dentro del contexto de suministros médicos.
                        13. Si el usuario menciona que tiene un presupuesto amplio, sugiere productos premium o de alta calidad, pero siempre dentro del contexto de suministros médicos.
                        14. Si el usuario menciona que tiene una urgencia o necesidad inmediata, sugiere productos con envío rápido o disponibilidad inmediata, pero siempre dentro del contexto de suministros médicos.
                        15. si el usuario te pide que hagas algo fuera de contexto, como contar un chiste o hablar de deportes, hacer tareas etc, recuérdale amablemente que estás enfocado en ayudar con compras médicas, pero hazlo con un toque de humor y emojis para mantener la conversación amena." 
            } 
        }
    },
    contents = new[]
    {
        new { parts = new[] { new { text = mensajeUsuario } } }
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