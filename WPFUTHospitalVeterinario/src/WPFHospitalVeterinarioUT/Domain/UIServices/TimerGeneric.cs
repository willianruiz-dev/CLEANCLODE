using System.Timers;

namespace Domain.UIServices
{
    public class TimerGeneric
    {
        public Action<string>? CallBackTick;
        public Action? CallBackTimeOut;
        public Action? CallBackStop;

        public int Minutes;
        public int Seconds;

        private System.Timers.Timer _timer;

        public TimerGeneric(string stringTimer)
        {
            try
            {
                Minutes = int.Parse(stringTimer.Split(':')[0]);
                Seconds = int.Parse(stringTimer.Split(':')[1]);

                _timer = new System.Timers.Timer();
                _timer.Interval = 1000;
                _timer.Elapsed += new ElapsedEventHandler(TimerTick);

                CallBackStop = () =>
                {
                    _timer.Elapsed -= TimerTick;
                    _timer.Stop();
                };
                _timer.Start();

            }
            catch
            {
            }
        }

        private void TimerTick(object? sender, ElapsedEventArgs e)
        {
            try
            {
                if (Minutes >= 1)
                {
                    Seconds--;
                    if (Seconds < 0)
                    {
                        Minutes--;
                        Seconds = 59;
                    }
                }
                else
                {
                    Seconds--;
                    if (Seconds < 0)
                    {
                        _timer.Stop();
                        CallBackTimeOut?.Invoke();
                    }
                }

                string stringTimer = string.Concat(Minutes.ToString().PadLeft(2, '0'), ":", Seconds.ToString().PadLeft(2, '0'));
                CallBackTick?.Invoke(stringTimer);
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Ocurrió un error en tiempo de ejecución {ex.Message}", ex);
            }
        }
    }
}
