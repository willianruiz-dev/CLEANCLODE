// ────────────────────────────────────────────────────────────────────────────────
// Envuelve TimerGeneric para las vistas. Cada pantalla con cuenta regresiva tenía
// su propia copia de GoTimer/StopTimer (crear, asignar callbacks, disponer), y este
// servicio concentra ese ciclo de vida para que la vista solo declare el tiempo,
// lo que muestra en pantalla y qué hacer cuando se agota.
// ────────────────────────────────────────────────────────────────────────────────
using System.Windows;
using System.Windows.Threading;

namespace Domain.UIServices
{
    public sealed class TimerService : IDisposable
    {
        private TimerGeneric? _timer;

        /// <summary>
        /// Inicia la cuenta regresiva. Si había un temporizador activo lo reemplaza.
        /// </summary>
        /// <param name="duration">Duración en formato mm:ss.</param>
        /// <param name="onTick">Cada segundo recibe el tiempo restante en formato mm:ss.</param>
        /// <param name="onTimeout">Se ejecuta cuando se agota el tiempo.</param>
        public void Start(string duration, Action<string> onTick, Action onTimeout)
        {
            try
            {
                _timer?.Dispose();
                _timer = new TimerGeneric(duration);

                var dispatcher = Application.Current?.Dispatcher;

                // El tick no bloquea al temporizador; el timeout se ejecuta de forma síncrona
                // en el hilo de interfaz, igual que antes.
                _timer.CallBackTick = value => RunOnUiThread(dispatcher, () => onTick(value), blocking: false);
                _timer.CallBackTimeOut = () => RunOnUiThread(dispatcher, onTimeout, blocking: true);
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, "No se pudo iniciar el temporizador.", ex);
            }
        }

        public void Stop()
        {
            try
            {
                _timer?.Dispose();
                _timer = null;
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, "No se pudo detener el temporizador.", ex);
            }
        }

        public void Dispose() => Stop();

        private static void RunOnUiThread(Dispatcher? dispatcher, Action action, bool blocking)
        {
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                action();
                return;
            }

            if (blocking)
                dispatcher.Invoke(action);
            else
                dispatcher.InvokeAsync(action);
        }
    }
}
