using ApiService.Models;
using ApiService.QueueModels;
using Domain;
using Domain.Enumerables;
using Domain.UIServices;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace WPFHospitalVeterinarioUT.ApiService
{
    public static class ApiDashboard
    {
        private static string _baseAddress;
        private static string _keyId;
        private static HttpClient _client;
        private static string? _token;
        private static RequestQueue _requestsQueue;
        private static readonly HospitalTransactionService _hospitalTransactions = new();

        static ApiDashboard()
        {
            _baseAddress = AppConfig.Get("apiBaseAddress");
            _keyId = AppConfig.Get("apiKeyId");
            _client = new HttpClient();
            _client.BaseAddress = new Uri(_baseAddress);
            _client.DefaultRequestHeaders.Add("DashboardKeyId", _keyId);
            _client.Timeout = TimeSpan.FromMilliseconds(10000);
            _requestsQueue = new RequestQueue();
        }

        public static async Task<bool> Login()
        {

            var oCredentials = new LoginDto
            {
                userName = AppConfig.Get("username"),
                password = AppConfig.Get("pwd")
            };

            string credentials = JsonConvert.SerializeObject(oCredentials);

            var content = new StringContent(credentials, Encoding.UTF8, "Application/json");
            var endpoint = AppConfig.Get("Login");

            var response = await _client.PostAsync(endpoint, content);

            var result = await response.Content.ReadAsStringAsync();
            if (result == null)
            {
                EventLogger.SaveLog(EventType.Error, "No se obtuvo contenido de la api");
                return false;

            }

            var requestresponse = JsonConvert.DeserializeObject<ApiResponse<string>>(result);
            if (requestresponse == null)
            {
                EventLogger.SaveLog(EventType.Error, "Error deserializando la respuesta");
                return false;
            }

            if (requestresponse.statusCode == 200)
            {
                _token = requestresponse.response;
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                return true;
            }

            EventLogger.SaveLog(EventType.Error, "Api no respondió satisfactoriamente", requestresponse);
            return false;

        }

        public static async Task<bool> Validate()
        {
            var endpoint = AppConfig.Get("Validate");

            var response = await _client.GetAsync(endpoint);

            var result = await response.Content.ReadAsStringAsync();
            if (result == null)
            {
                EventLogger.SaveLog(EventType.Error, "No se obtuvo respuesta del servicio");
                return false;

            }

            var requestresponse = JsonConvert.DeserializeObject<ApiResponse<bool>>(result);
            if (requestresponse == null)
            {
                EventLogger.SaveLog(EventType.Error, "Error deserializando la respuesta");
                return false;
            }

            if (requestresponse.statusCode == 200)
            {
                return requestresponse.response;
            }

            EventLogger.SaveLog(EventType.Error, "Api no respondió satisfactoriamente", requestresponse);
            return false;

        }

        public static async Task<TransactionDto?> CreateTransaction()
        {
            var ts = Transaction.Instance;

            ts.transactionProcess.EstadoTransaccion = StateTransaction.Iniciada;
            var transactionToCreate = new TransactionDto
            {
                Document = ts.paymentProcess.Documento,
                Reference = ts.paymentProcess.Referencia,
                Product = ts.transactionProcess.TipoRecaudo,
                TotalAmount = Convert.ToDouble(ts.paymentProcess.Total),
                RealAmount = Convert.ToDouble(ts.paymentProcess.TotalSinRedondear),
                IncomeAmount = 0,
                ReturnAmount = 0,
                Description = ts.paymentProcess.Descripcion ?? string.Empty,
                IdStateTransaction = (int)ts.transactionProcess.EstadoTransaccion,
                StateTransaction = ts.transactionProcess.EstadoTransaccion.ToString(),
                IdTypeTransaction = (int)ts.transactionProcess.TipoTransaccion,
                IdTypePayment = (int)ts.transactionProcess.TipoPago,
            };



            int tries = 2;
            while (tries > 0)
            {
                tries--;

                string payload = JsonConvert.SerializeObject(transactionToCreate);

                var content = new StringContent(payload, Encoding.UTF8, "Application/json");
                var url = AppConfig.Get("Transaction");

                EventLogger.SaveLog(EventType.Info, "Petición: Creación de transacción Dashboard", transactionToCreate);
                var response = await _client.PostAsync(url, content);

                var result = await response.Content.ReadAsStringAsync();
                if (result == null)
                {
                    EventLogger.SaveLog(EventType.Error, "No se obtuvo contenido de la api");
                    continue;
                }

                var requestresponse = JsonConvert.DeserializeObject<ApiResponse<TransactionDto>>(result);
                if (requestresponse == null)
                {
                    EventLogger.SaveLog(EventType.Error, "Error deserializando la respuesta");
                    continue;
                }

                if (requestresponse.statusCode == 200)
                {
                    var transactionCreated = requestresponse.response;
                    ts.transactionProcess.ApiDto = transactionCreated;
                    ts.IdTransaccionApi = transactionCreated.Id;

                    var hospitalTransaction = await _hospitalTransactions.CreateAsync(
                        ToHospitalTransaction(transactionCreated));
                    if (hospitalTransaction != null)
                        ts.IdTransaccionUt = hospitalTransaction.TransactionId;

                    EventLogger.SaveLog(EventType.Info, "Respuesta: Creación de transacción Dashboard", requestresponse);
                    return requestresponse.response;
                }

                EventLogger.SaveLog(EventType.Error, "Api no respondió satisfactoriamente", requestresponse);
            }

            return null;

        }

        public static void UpdateTransaction()
        {
            var ts = Transaction.Instance;

            var transactionToUpdate = ts.transactionProcess.ApiDto;

            transactionToUpdate.IdStateTransaction = (int)ts.transactionProcess.EstadoTransaccion;
            transactionToUpdate.StateTransaction = ts.transactionProcess.EstadoTransaccion.ToString();
            transactionToUpdate.Description = ts.paymentProcess.Descripcion;
            transactionToUpdate.IncomeAmount = (double)ts.paymentProcess.TotalIngresado;
            transactionToUpdate.ReturnAmount = (double)ts.paymentProcess.TotalDevuelta;

            _requestsQueue.Enqueue(async () =>
            {
                string payload = JsonConvert.SerializeObject(transactionToUpdate);

                var content = new StringContent(payload, Encoding.UTF8, "Application/json");
                var url = AppConfig.Get("Transaction");

                EventLogger.SaveLog(EventType.Info, "Petición: Actualización de transacción Dashboard", transactionToUpdate);
                var response = await _client.PutAsync(url, content);

                var result = await response.Content.ReadAsStringAsync();
                if (result == null)
                {
                    EventLogger.SaveLog(EventType.Error, "No se obtuvo contenido de la api");
                    return null;
                }

                var requestresponse = JsonConvert.DeserializeObject<ApiResponse<TransactionDto>>(result);
                if (requestresponse == null)
                {
                    EventLogger.SaveLog(EventType.Error, "Error deserializando la respuesta");
                    return null;
                }

                if (requestresponse.statusCode == 200)
                {
                    var transactionUpdated = requestresponse.response;
                    ts.transactionProcess.ApiDto = transactionUpdated;

                    if (ts.IdTransaccionUt > 0)
                    {
                        var hospitalTransaction = ToHospitalTransaction(transactionUpdated);
                        hospitalTransaction.TransactionId = ts.IdTransaccionUt;
                        await _hospitalTransactions.UpdateAsync(hospitalTransaction);
                    }

                    EventLogger.SaveLog(EventType.Info, "Respuesta: Actualización de transacción Dashboard", requestresponse);
                    return requestresponse.response;
                }

                EventLogger.SaveLog(EventType.Error, "Api no respondió satisfactoriamente", requestresponse);
                return null;
            });

        }

        public static void CreateTransactionDetail(TypeOperation op, int value, int quantity)
        {
            var detail = new TransactionDetailDto
            {
                IdTransaction = Transaction.Instance.transactionProcess.ApiDto.Id,
                CurrencyDenomination = value,
                IdTypeOperation = (int)op,
                Quantity = quantity
            };



            _requestsQueue.Enqueue(async () =>
            {
                string payload = JsonConvert.SerializeObject(detail);

                var content = new StringContent(payload, Encoding.UTF8, "Application/json");
                var url = AppConfig.Get("TransactionDetails");

                EventLogger.SaveLog(EventType.Info, "Petición: Creación de detalle de transacción Dashboard", detail);
                var response = await _client.PostAsync(url, content);

                var result = await response.Content.ReadAsStringAsync();
                if (result == null)
                {
                    EventLogger.SaveLog(EventType.Error, "No se obtuvo contenido de la api");
                    return null;
                }

                var requestresponse = JsonConvert.DeserializeObject<ApiResponse<List<TransactionDetailDto>>>(result);
                if (requestresponse == null)
                {
                    EventLogger.SaveLog(EventType.Error, "Error deserializando la respuesta");
                    return null;
                }

                if (requestresponse.statusCode == 200)
                {
                    var listTransactionsCreated = requestresponse.response ?? new List<TransactionDetailDto>();
                    EventLogger.SaveLog(EventType.Info, "Respuesta: Creación de detalle de transacción Dashboard", requestresponse);

                    foreach (var createdDetail in listTransactionsCreated)
                    {
                        var hospitalDetail = new HospitalTransactionDetailDto
                        {
                            IdApi = createdDetail.Id.ToString(),
                            IdTransaction = Transaction.Instance.IdTransaccionUt,
                            IdCurrencyDenomination = createdDetail.IdCurrencyDenomination,
                            CurrencyDenomination = createdDetail.CurrencyDenomination.ToString(),
                            IdTypeOperation = createdDetail.IdTypeOperation,
                            TypeOperation = createdDetail.TypeOperation,
                            DateCreated = createdDetail.DateCreated,
                            DateUpdated = createdDetail.DateUpdated,
                            // Swagger exige la navegación Transaction. Se envía solo la llave
                            // de la fila ya creada para no intentar insertar nuevamente el padre.
                            Transaction = new HospitalTransactionDto
                            {
                                TransactionId = Transaction.Instance.IdTransaccionUt
                            }
                        };

                        await _hospitalTransactions.CreateDetailAsync(hospitalDetail);
                    }

                    return listTransactionsCreated;
                }

                EventLogger.SaveLog(EventType.Error, "Api no respondió satisfactoriamente", requestresponse);
                return null;
            });
        }

        public static void SetTransactionRating(int idTran, int rating)
        {

            var ratingDto = new TransactionRatingDto
            {
                IdTransaction = idTran,
                Rating = rating,
                DateCreated = DateTime.Now,
            };


            _requestsQueue.Enqueue(async () =>
            {
                string payload = JsonConvert.SerializeObject(ratingDto);

                var content = new StringContent(payload, Encoding.UTF8, "Application/json");
                var url = AppConfig.Get("TransactionRating");

                EventLogger.SaveLog(EventType.Info, "Petición: Creación Calificación de transacción Dashboard", ratingDto);
                var response = await _client.PostAsync(url, content);

                var result = await response.Content.ReadAsStringAsync();
                if (result == null)
                {
                    EventLogger.SaveLog(EventType.Error, "No se obtuvo contenido de la api");
                    return null;
                }

                var requestresponse = JsonConvert.DeserializeObject<ApiResponse<TransactionRatingDto>>(result);
                if (requestresponse == null)
                {
                    EventLogger.SaveLog(EventType.Error, "Error deserializando la respuesta");
                    return null;
                }

                if (requestresponse.statusCode == 200)
                {
                    EventLogger.SaveLog(EventType.Info, $"Respuesta: Calificación guardada en Dashboard - Transacción {idTran}, Rating {rating}");
                    return requestresponse.response;
                }

                EventLogger.SaveLog(EventType.Error, $"Api no respondió satisfactoriamente (HTTP {(int)response.StatusCode})", requestresponse);
                return null;
            });

        }

        private static HospitalTransactionDto ToHospitalTransaction(TransactionDto source)
        {
            return new HospitalTransactionDto
            {
                IdApi = source.Id.ToString(),
                Document = source.Document,
                Reference = source.Reference,
                Product = source.Product,
                TotalAmount = source.TotalAmount,
                RealAmount = source.RealAmount,
                IncomeAmount = source.IncomeAmount,
                ReturnAmount = source.ReturnAmount,
                Description = source.Description,
                IdStateTransaction = source.IdStateTransaction,
                StateTransaction = source.StateTransaction,
                DateCreated = source.DateCreated,
                DateUpdated = source.DateUpdated
            };
        }

    }
}
