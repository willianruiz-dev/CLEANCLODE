using Domain;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Sockets;

namespace ApiService.QueueModels
{
    public delegate Task<object?> RequestDelegate();

    /// <summary>
    /// Ejecuta las escrituras a la API en orden y con un único consumidor.
    /// Los fallos transitorios se reintentan con espera incremental para evitar ciclos intensivos.
    /// </summary>
    public sealed class RequestQueue
    {
        private const int MaxAttempts = 3;
        private readonly ConcurrentQueue<QueuedRequest> _requests = new();
        private readonly object _workerLock = new();
        private Task? _worker;

        public void Enqueue(RequestDelegate requestFunc)
        {
            ArgumentNullException.ThrowIfNull(requestFunc);
            _requests.Enqueue(new QueuedRequest(requestFunc));
            EnsureWorkerIsRunning();
        }

        private void EnsureWorkerIsRunning()
        {
            lock (_workerLock)
            {
                if (_worker == null || _worker.IsCompleted)
                    _worker = ProcessQueueAsync();
            }
        }

        private async Task ProcessQueueAsync()
        {
            while (_requests.TryDequeue(out var request))
            {
                try
                {
                    await request.Callback().ConfigureAwait(false);
                }
                catch (Exception ex) when (IsTransient(ex) && request.Attempt < MaxAttempts)
                {
                    request.Attempt++;
                    EventLogger.SaveLog(
                        EventType.Warning,
                        $"Petición API en cola falló temporalmente. Reintento {request.Attempt} de {MaxAttempts}.",
                        ex);

                    await Task.Delay(TimeSpan.FromSeconds(request.Attempt * 2)).ConfigureAwait(false);
                    _requests.Enqueue(request);
                }
                catch (Exception ex)
                {
                    EventLogger.SaveLog(
                        EventType.Error,
                        "No fue posible ejecutar una petición API en cola.",
                        ex);
                }
            }

            // Cierra la carrera en la que se encola una solicitud justo cuando finaliza el bucle.
            lock (_workerLock)
            {
                if (!_requests.IsEmpty)
                    _worker = ProcessQueueAsync();
            }
        }

        private static bool IsTransient(Exception exception) =>
            exception is HttpRequestException { InnerException: SocketException } ||
            exception is TaskCanceledException;

        private sealed class QueuedRequest
        {
            public QueuedRequest(RequestDelegate callback) => Callback = callback;

            public RequestDelegate Callback { get; }
            public int Attempt { get; set; } = 1;
        }
    }
}
