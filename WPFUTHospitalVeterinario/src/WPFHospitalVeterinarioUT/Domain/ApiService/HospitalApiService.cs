using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WPFHospitalVeterinarioUT.Domain.ApiService
{
    public class HospitalApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public HospitalApiService()
        {
            _httpClient = new HttpClient();
            _baseUrl = ConfigurationManager.AppSettings["HospitalApiBaseUrl"] ?? "https://localhost:7287/api/Hospital/";
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public HospitalApiService(string baseUrl)
        {
            _httpClient = new HttpClient();
            _baseUrl = baseUrl;
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        // Métodos para UserPersonalInfo
        public async Task<UserPersonalInfoDto?> GetUserPersonalInfoAsync(string document)
        {
            try
            {
                var response = await _httpClient.GetAsync($"UserPersonalInfo/{document}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<UserPersonalInfoDto>(json, GetJsonOptions());
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener información del usuario: {ex.Message}", ex);
            }
        }

        public async Task<UserPersonalInfoDto> CreateOrUpdateUserPersonalInfoAsync(UserPersonalInfoDto userInfo)
        {
            try
            {
                var json = JsonSerializer.Serialize(userInfo, GetJsonOptions());
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("UserPersonalInfo/CreateOrUpdate", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<UserPersonalInfoDto>(responseJson, GetJsonOptions())!;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al crear/actualizar usuario: {ex.Message}", ex);
            }
        }

        public async Task<List<UserPersonalInfoDto>> SearchUserPersonalInfoAsync(string query)
        {
            try
            {
                var response = await _httpClient.GetAsync($"UserPersonalInfo/Search?query={Uri.EscapeDataString(query)}");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<UserPersonalInfoDto>>(json, GetJsonOptions()) ?? new List<UserPersonalInfoDto>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al buscar usuarios: {ex.Message}", ex);
            }
        }

        // Métodos para Transactions
        public async Task<TransactionDto> CompleteTransactionAsync(CompleteTransactionRequestDto request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request, GetJsonOptions());
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("Transactions/Complete", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TransactionDto>(responseJson, GetJsonOptions())!;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al completar transacción: {ex.Message}", ex);
            }
        }

        public async Task<TransactionDto?> GetTransactionAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"Transactions/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<TransactionDto>(json, GetJsonOptions());
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener transacción: {ex.Message}", ex);
            }
        }

        public async Task<List<TransactionDto>> GetTransactionsByDocumentAsync(string document)
        {
            try
            {
                var response = await _httpClient.GetAsync($"Transactions/ByDocument/{document}");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<TransactionDto>>(json, GetJsonOptions()) ?? new List<TransactionDto>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener transacciones por documento: {ex.Message}", ex);
            }
        }

        public async Task<List<TransactionDto>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var start = startDate.ToString("yyyy-MM-dd");
                var end = endDate.ToString("yyyy-MM-dd");
                var response = await _httpClient.GetAsync($"Transactions/ByDateRange?startDate={start}&endDate={end}");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<TransactionDto>>(json, GetJsonOptions()) ?? new List<TransactionDto>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener transacciones por rango de fechas: {ex.Message}", ex);
            }
        }

        // Método para probar la conexión con la API
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("UserPersonalInfo");
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

    // DTOs para la comunicación con la API
    public class UserPersonalInfoDto
    {
        public string Document { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class TransactionDto
    {
        public int TransactionId { get; set; }
        public int IdApi { get; set; }
        public string? Document { get; set; }
        public string? Reference { get; set; }
        public string? Product { get; set; }
        public double TotalAmount { get; set; }
        public double RealAmount { get; set; }
        public double IncomeAmount { get; set; }
        public double ReturnAmount { get; set; }
        public string? Description { get; set; }
        public int IdStateTransaction { get; set; }
        public string? StateTransaction { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public List<TransactionDetailDto> TransactionDetails { get; set; } = new();
    }

    public class TransactionDetailDto
    {
        public int TranDetailId { get; set; }
        public int IdApi { get; set; }
        public int IdTransaction { get; set; }
        public int IdCurrencyDenomination { get; set; }
        public int CurrencyDenomination { get; set; }
        public int IdTypeOperation { get; set; }
        public string TypeOperation { get; set; } = string.Empty;
        public DateTime? DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
    }

    public class CompleteTransactionRequestDto
    {
        public string Document { get; set; } = string.Empty;
        public string Names { get; set; } = string.Empty;
        public string LastNames { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PetName { get; set; } = string.Empty;
        public string PetType { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }
        public List<TransactionDetailRequestDto>? TransactionDetails { get; set; }
    }

    public class TransactionDetailRequestDto
    {
        public string BillDenomination { get; set; } = string.Empty;
        public int BillQuantity { get; set; }
        public decimal BillAmount { get; set; }
    }
}
