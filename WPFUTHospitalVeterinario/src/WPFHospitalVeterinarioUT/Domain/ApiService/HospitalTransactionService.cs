using ApiService.Models;
using Domain;
using Newtonsoft.Json;
using System.Text;

namespace WPFHospitalVeterinarioUT.ApiService
{
    /// <summary>
    /// Sincroniza el resultado del Dashboard con la API de Universidad del Tolima.
    /// La aplicación nunca accede directamente a SQL Server.
    /// </summary>
    public sealed class HospitalTransactionService
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

        public async Task<HospitalTransactionDto?> CreateAsync(
            HospitalTransactionDto transaction,
            CancellationToken cancellationToken = default)
        {
            return await SendAsync<HospitalTransactionDto>(
                HttpMethod.Post,
                "Transaction",
                transaction,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> UpdateAsync(
            HospitalTransactionDto transaction,
            CancellationToken cancellationToken = default)
        {
            if (transaction.TransactionId <= 0)
                return false;

            var result = await SendAsync<object>(
                HttpMethod.Put,
                $"Transaction/{transaction.TransactionId}",
                transaction,
                cancellationToken,
                allowEmptyResponse: true).ConfigureAwait(false);
            return result != null;
        }

        public async Task<HospitalTransactionDetailDto?> CreateDetailAsync(
            HospitalTransactionDetailDto detail,
            CancellationToken cancellationToken = default)
        {
            if (detail.IdTransaction <= 0)
            {
                EventLogger.SaveLog(EventType.Error,
                    "No se envió el detalle a UT porque no existe el identificador de la transacción padre.");
                return null;
            }

            return await SendAsync<HospitalTransactionDetailDto>(
                HttpMethod.Post,
                "TransactionDetail",
                detail,
                cancellationToken).ConfigureAwait(false);
        }

        private static async Task<T?> SendAsync<T>(
            HttpMethod method,
            string endpoint,
            object payload,
            CancellationToken cancellationToken,
            bool allowEmptyResponse = false) where T : class
        {
            try
            {
                var json = JsonConvert.SerializeObject(payload);
                using var request = new HttpRequestMessage(method, endpoint)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                using var response = await Client.SendAsync(request, cancellationToken).ConfigureAwait(false);
                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    EventLogger.SaveLog(EventType.Error,
                        $"La API UT respondió HTTP {(int)response.StatusCode} en {endpoint}.",
                        responseBody);
                    return null;
                }

                EventLogger.SaveLog(EventType.Info, $"Sincronización UT completada: {method} {endpoint}.");
                if (allowEmptyResponse && typeof(T) == typeof(object))
                    return new object() as T;

                return JsonConvert.DeserializeObject<T>(responseBody);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return null;
            }
            catch (Exception ex)
            {
                // La sincronización UT es secundaria respecto al cobro Dashboard.
                // No se propaga para impedir que la cola repita una operación Dashboard ya exitosa.
                EventLogger.SaveLog(EventType.Error,
                    $"Error comunicándose con la API UT en {endpoint}.", ex);
                return null;
            }
        }

    }
}
