// ────────────────────────────────────────────────────────────────────────────────
// Punto único para iniciar y detener la grabación de video de una transacción.
// El VideoRecorder vive en Transaction; este servicio centraliza su ciclo de vida
// (creación, reintentos de parada y liberación de recursos) para que las vistas
// solo llamen a StartAsync/StopAsync y no repitan la misma lógica.
// ────────────────────────────────────────────────────────────────────────────────
using Domain.UIServices;

namespace Domain.Peripherals.Recorder
{
    public sealed class RecordingService
    {
        private const int MaxStopAttempts = 3;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(500);

        private static readonly Lazy<RecordingService> _instance = new(() => new RecordingService());

        public static RecordingService Instance => _instance.Value;

        private RecordingService() { }

        /// <summary>Indica si hay una grabación en curso para la transacción actual.</summary>
        public bool IsRecording => Transaction.Instance.videoRecorder?.IsRecording ?? false;

        /// <summary>
        /// Inicia la grabación de la transacción actual. Si no existe un VideoRecorder
        /// lo crea; si ya estaba grabando no hace nada.
        /// </summary>
        public async Task<bool> StartAsync(int source = 0)
        {
            try
            {
                var transaction = Transaction.Instance;
                var recorder = transaction.videoRecorder ??= new VideoRecorder(transaction);
                var started = await recorder.StartAsync(source);

                EventLogger.SaveLog(
                    started ? EventType.Info : EventType.Warning,
                    started
                        ? "Grabación de video iniciada."
                        : "No se pudo iniciar la grabación; la transacción continuará sin video.");

                return started;
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, "Error inicializando la grabación de video.", ex);
                return false;
            }
        }

        /// <summary>
        /// Detiene y libera la grabación de la transacción actual. Reintenta la parada
        /// hasta <see cref="MaxStopAttempts"/> veces antes de rendirse.
        /// </summary>
        public async Task<bool> StopAsync()
        {
            var transaction = Transaction.Instance;
            var recorder = transaction.videoRecorder;
            if (recorder == null)
            {
                EventLogger.SaveLog(EventType.Info, "No hay grabación de video activa para detener.");
                return true;
            }

            var stopped = false;
            try
            {
                for (var attempt = 1; attempt <= MaxStopAttempts && !stopped; attempt++)
                {
                    stopped = await recorder.StopAsync();
                    EventLogger.SaveLog(EventType.Info,
                        $"Intento {attempt} de detener la grabación: {(stopped ? "exitoso" : "fallido")}");
                    if (!stopped)
                        await Task.Delay(RetryDelay);
                }
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, "Error al detener la grabación de video.", ex);
                return false;
            }

            if (!stopped)
            {
                // Igual que antes: si el hilo de captura sigue activo no se liberan los
                // recursos nativos; se conserva el grabador por si se reintenta luego.
                EventLogger.SaveLog(EventType.Warning,
                    "No fue posible detener la grabación de video; se conserva el grabador para un nuevo intento.");
                return false;
            }

            try
            {
                recorder.Dispose();
                if (ReferenceEquals(transaction.videoRecorder, recorder))
                    transaction.videoRecorder = null;
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, "Error liberando los recursos de la grabación de video.", ex);
            }

            return true;
        }
    }
}
