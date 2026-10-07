using System.Diagnostics;
using System.Globalization;
using System.Timers;

namespace Domain.UIServices
{
    /// <summary>
    /// Temporizador regresivo basado en tiempo transcurrido real.
    /// Evita ticks superpuestos y deriva cuando un callback tarda en ejecutarse.
    /// </summary>
    public sealed class TimerGeneric : IDisposable
    {
        private readonly object _sync = new();
        private readonly System.Timers.Timer _timer;
        private readonly Stopwatch _stopwatch = new();
        private readonly TimeSpan _duration;
        private bool _stopped;

        public Action<string>? CallBackTick { get; set; }
        public Action? CallBackTimeOut { get; set; }

        // Se conserva para compatibilidad con las vistas existentes.
        public Action? CallBackStop { get; }

        public TimerGeneric(string stringTimer)
        {
            _duration = ParseDuration(stringTimer);
            _timer = new System.Timers.Timer(1000)
            {
                AutoReset = false
            };
            _timer.Elapsed += TimerTick;
            CallBackStop = Stop;

            _stopwatch.Start();
            _timer.Start();
        }

        private static TimeSpan ParseDuration(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("El tiempo no puede estar vacío.", nameof(value));

            var parts = value.Split(':');
            if (parts.Length != 2 ||
                !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) ||
                !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) ||
                minutes < 0 || seconds is < 0 or > 59)
            {
                throw new FormatException($"Formato de temporizador inválido: '{value}'. Se esperaba mm:ss.");
            }

            return TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
        }

        private void TimerTick(object? sender, ElapsedEventArgs e)
        {
            Action<string>? tickCallback = null;
            Action? timeoutCallback = null;
            string? displayTime = null;

            lock (_sync)
            {
                if (_stopped)
                    return;

                var remaining = _duration - _stopwatch.Elapsed;
                if (remaining <= TimeSpan.Zero)
                {
                    _stopped = true;
                    _stopwatch.Stop();
                    timeoutCallback = CallBackTimeOut;
                }
                else
                {
                    // Ceiling conserva el comportamiento visual: 03:00 pasa a 02:59
                    // después del primer segundo, sin perder tiempo por deriva del hilo.
                    var totalSeconds = (int)Math.Ceiling(remaining.TotalSeconds);
                    displayTime = $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
                    tickCallback = CallBackTick;
                }
            }

            try
            {
                if (displayTime != null)
                    tickCallback?.Invoke(displayTime);
                else
                    timeoutCallback?.Invoke();

                if (displayTime != null)
                {
                    lock (_sync)
                    {
                        if (!_stopped)
                            _timer.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, "Error ejecutando callback del temporizador.", ex);
            }
        }

        public void Stop()
        {
            lock (_sync)
            {
                if (_stopped)
                    return;

                _stopped = true;
                _stopwatch.Stop();
                _timer.Stop();
            }
        }

        public void Dispose()
        {
            Stop();
            _timer.Elapsed -= TimerTick;
            _timer.Dispose();
            CallBackTick = null;
            CallBackTimeOut = null;
        }
    }
}
