using ApiService.Models;
using Domain;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Text;

namespace WPFHospitalVeterinarioUT.ApiService
{
    /// <summary>
    /// Acceso exclusivo por API a la información personal del usuario.
    /// No utiliza SQLite, Entity Framework ni conexión directa a SQL Server.
    /// </summary>
    public sealed class HospitalUserService
    {
        private static readonly HttpClient Client = CreateClient();

        private static HttpClient CreateClient()
        {
            var baseUrl = AppConfig.Get("HospitalApiBaseUrl");
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("No se configuró HospitalApiBaseUrl.");

            return new HttpClient
            {
                BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/"),
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        public async Task<UserPersonalInfoDto?> GetByDocumentAsync(
            string document,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(document))
                return null;

            using var response = await Client.GetAsync(
                $"User/{Uri.EscapeDataString(document)}",
                cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // Antes este caso no dejaba rastro en ningún log: la aplicación simplemente
                // pedía los datos en el formulario y nadie podía saber si el documento existía.
                EventLogger.SaveLog(EventType.Info,
                    $"El documento {document} no está registrado en la API UT (HTTP 404); los datos se solicitan en el formulario.");
                return null;
            }

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                EventLogger.SaveLog(EventType.Error,
                    $"La API de usuarios respondió HTTP {(int)response.StatusCode} al consultar el documento.",
                    body);
                return null;
            }

            var user = JsonConvert.DeserializeObject<UserPersonalInfoDto>(body);
            EventLogger.SaveLog(EventType.Info,
                user == null
                    ? "La API no devolvió información válida para el documento consultado."
                    : "Usuario encontrado por documento en la API.");
            return user;
        }

        public async Task<bool> CreateOrUpdateAsync(
            UserPersonalInfoDto user,
            CancellationToken cancellationToken = default)
        {
            var payload = JsonConvert.SerializeObject(user);
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await Client.PostAsync("User", content, cancellationToken)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                EventLogger.SaveLog(EventType.Info, "Información personal almacenada mediante la API.");

                // La API puede responder OK sin haber guardado realmente (es el caso del
                // documento nuevo). Una consulta posterior, en segundo plano, confirma que el
                // documento quedó registrado; si no aparece, queda dicho aquí mismo.
                if (!string.IsNullOrWhiteSpace(user.Document))
                    _ = VerifyStoredAsync(user.Document);

                return true;
            }

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            EventLogger.SaveLog(EventType.Error,
                $"La API no pudo almacenar la información personal. HTTP {(int)response.StatusCode}.",
                body);
            return false;
        }

        /// <summary>
        /// Comprueba, en segundo plano, que el documento que la API dijo haber guardado
        /// realmente quedó registrado. No interfiere con el flujo del formulario.
        /// </summary>
        private async Task VerifyStoredAsync(string document)
        {
            try
            {
                // Pequeña espera por si la propia API tarda en reflejar el guardado.
                await Task.Delay(1500).ConfigureAwait(false);

                var stored = await GetByDocumentAsync(document).ConfigureAwait(false);

                if (stored == null)
                {
                    EventLogger.SaveLog(EventType.Warning,
                        $"Atención: la API confirmó el guardado del documento {document}, pero una consulta inmediata no lo encuentra registrado.");
                }
                else
                {
                    // Se registra también lo que la API devuelve, para poder ver si los datos
                    // quedaron completos y no solo si el documento existe.
                    EventLogger.SaveLog(EventType.Info,
                        $"Verificación: el documento {document} sí quedó registrado en la API UT. Datos devueltos:",
                        stored);
                }
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Warning,
                    $"No fue posible verificar el documento {document} en la API UT.", ex);
            }
        }
    }
}
