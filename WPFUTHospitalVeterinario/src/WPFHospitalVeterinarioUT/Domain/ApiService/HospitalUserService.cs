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
                return null;

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
                return true;
            }

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            EventLogger.SaveLog(EventType.Error,
                $"La API no pudo almacenar la información personal. HTTP {(int)response.StatusCode}.",
                body);
            return false;
        }
    }
}
