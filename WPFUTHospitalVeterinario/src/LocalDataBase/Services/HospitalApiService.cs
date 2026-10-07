using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LocalDataBase.Services
{
    public class HospitalApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public HospitalApiService()
        {
            var handler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            _httpClient = new HttpClient(handler);
            _baseUrl = ConfigurationManager.AppSettings["HospitalApiBaseUrl"] ?? "https://localhost:7287/api/Hospital/";
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public HospitalApiService(string baseUrl)
        {
            _httpClient = new HttpClient();
            _baseUrl = baseUrl;
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task<UserPersonalInfoDto> GetUserAsync(string document)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[API] Consultando usuario: {document}");
                System.Diagnostics.Debug.WriteLine($"[API] URL Base: {_baseUrl}");
                
                var response = await _httpClient.GetAsync($"User/{document}");
                
                System.Diagnostics.Debug.WriteLine($"[API] Status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[API] Response: {json}");
                    return JsonSerializer.Deserialize<UserPersonalInfoDto>(json, GetJsonOptions());
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[API] Error: {errorContent}");
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[API] Exception: {ex.Message}");
                return null;
            }
        }

        public async Task<UserPersonalInfoDto> CreateOrUpdateUserAsync(UserPersonalInfoDto userInfo)
        {
            try
            {
                var json = JsonSerializer.Serialize(userInfo, GetJsonOptions());
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("User", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<UserPersonalInfoDto>(responseJson, GetJsonOptions());
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<TransactionDto> UpdateTransactionAsync(int transactionId, TransactionDto transaction)
        {
            try
            {
                var json = JsonSerializer.Serialize(transaction, GetJsonOptions());
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                System.Diagnostics.Debug.WriteLine($"[API] Actualizando transacción ID: {transactionId}");
                System.Diagnostics.Debug.WriteLine($"[API] Datos: {json}");
                
                var response = await _httpClient.PutAsync($"Transaction/{transactionId}", content);
                
                System.Diagnostics.Debug.WriteLine($"[API] Update Status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    return transaction; // PUT devuelve NoContent, retornamos el objeto enviado
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[API] Update Error: {errorContent}");
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[API] Update Exception: {ex.Message}");
                return null;
            }
        }

        public async Task<List<UserPersonalInfoDto>> SearchUsersAsync(string query)
        {
            try
            {
                var response = await _httpClient.GetAsync($"User/Search?query={Uri.EscapeDataString(query)}");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<UserPersonalInfoDto>>(json, GetJsonOptions()) ?? new List<UserPersonalInfoDto>();
            }
            catch (Exception)
            {
                return new List<UserPersonalInfoDto>();
            }
        }

        public async Task<TransactionDto> CreateTransactionAsync(TransactionDto transaction)
        {
            try
            {
                var json = JsonSerializer.Serialize(transaction, GetJsonOptions());
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("Transaction", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TransactionDto>(responseJson, GetJsonOptions());
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<TransactionDto> GetTransactionAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"Transaction/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<TransactionDto>(json, GetJsonOptions());
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<TransactionDto>> GetTransactionsByDocumentAsync(string document)
        {
            try
            {
                var response = await _httpClient.GetAsync($"Transactions/{document}");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<TransactionDto>>(json, GetJsonOptions()) ?? new List<TransactionDto>();
            }
            catch (Exception)
            {
                return new List<TransactionDto>();
            }
        }

        public async Task<TransactionDetailDto> CreateTransactionDetailAsync(TransactionDetailDto detail)
        {
            try
            {
                var json = JsonSerializer.Serialize(detail, GetJsonOptions());
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("TransactionDetail", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TransactionDetailDto>(responseJson, GetJsonOptions());
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("Test");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    // DTOs
    public class UserPersonalInfoDto
    {
        public string Document { get; set; }
        public string DocumentType { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
    }

    public class TransactionDto
    {
        public int TransactionId { get; set; }
        public string IdApi { get; set; }
        public string Document { get; set; }
        public string Reference { get; set; }
        public string Product { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RealAmount { get; set; }
        public decimal IncomeAmount { get; set; }
        public decimal ReturnAmount { get; set; }
        public string Description { get; set; }
        public int IdStateTransaction { get; set; }
        public string StateTransaction { get; set; }
        public int IdTypeTransaction { get; set; }
        public string? TypeTransaction { get; set; }
        public int IdTypePayment { get; set; }
        public string? TypePayment { get; set; }
        public int IdPayPad { get; set; }
        public string? PayPad { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
    }

    public class TransactionDetailDto
    {
        public int TranDetailId { get; set; }
        public string IdApi { get; set; }
        public int IdTransaction { get; set; }
        public int IdCurrencyDenomination { get; set; }
        public int CurrencyDenomination { get; set; }
        public int IdTypeOperation { get; set; }
        public string? TypeOperation { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
        public int Quantity { get; set; }
    }
}
