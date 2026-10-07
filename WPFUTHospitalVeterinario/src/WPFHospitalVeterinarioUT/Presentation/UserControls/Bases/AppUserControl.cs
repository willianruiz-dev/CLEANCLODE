using Domain;
using Domain.UIServices;
using System.Windows.Controls;
using System.Windows.Threading;
using VirtualKeyboard.Wpf;

namespace UI.Bases
{
    public class AppUserControl : UserControl
    {
        protected Navigator _nav = Navigator.Instance;

        private readonly TimerService _timer = new();
        private TextBlock? _timerDisplay;

        public AppUserControl()
        {
            // La cuenta regresiva de la pantalla se detiene siempre al salir de ella,
            // aunque la vista olvide llamar a StopTimer en su propio Unloaded.
            Unloaded += (_, _) => StopTimer();
        }

        #region Temporizador de inactividad

        /// <summary>Duración de la cuenta regresiva de esta pantalla (mm:ss). Vacío: la pantalla no usa temporizador.</summary>
        protected virtual string TimerDuration => string.Empty;

        /// <summary>TextBlock donde se pinta el tiempo restante; por convención es el "TxtTimer" del XAML.</summary>
        protected virtual TextBlock? TimerDisplay => _timerDisplay ??= FindName("TxtTimer") as TextBlock;

        /// <summary>Acción al agotarse el tiempo; cada pantalla decide a dónde volver.</summary>
        protected virtual void OnTimerTimeout() { }

        /// <summary>Inicia (o reinicia) la cuenta regresiva de esta pantalla.</summary>
        protected void StartTimer()
        {
            if (string.IsNullOrWhiteSpace(TimerDuration)) return;

            try
            {
                UpdateTimerDisplay(TimerDuration);
                _timer.Start(TimerDuration, onTick: UpdateTimerDisplay, onTimeout: OnTimerTimeout);
            }
            catch (Exception ex)
            {
                EventLogger.SaveLog(EventType.Error, $"Ocurrió un error iniciando el temporizador de la pantalla: {ex.Message}", ex);
            }
        }

        /// <summary>Detiene la cuenta regresiva de esta pantalla.</summary>
        protected void StopTimer() => _timer.Stop();

        private void UpdateTimerDisplay(string time)
        {
            if (TimerDisplay is { } display)
                display.Text = time;
        }

        #endregion

        protected void GoTo(UserControl view)
        {
            if (VKeyboard._windowHost != null)
            {
                VKeyboard._windowHost.Close();
            }
            _nav.NavigateTo(view);
        }
        protected void DisableView()
        {
            Dispatcher.Invoke((Action)delegate
            {
                this.Opacity = 0.3;
                this.IsEnabled = false;
            });
        }

        protected void EnableView()
        {
            Dispatcher.Invoke((Action)delegate
            {
                this.Opacity = 1;
                this.IsEnabled = true;
            });
        }
        public bool IsNumeric(string input)
        {
            return !string.IsNullOrEmpty(input) && input.All(char.IsDigit);
        }
        public string FindShortWord(string sentence)
        {
            // Dividir la oración en palabras usando los espacios como separadores
            string[] words = sentence.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Verificar si la oración está vacía
            if (words.Length == 0)
                return null;

            // Encontrar la palabra más corta usando LINQ
            string wordShort = words.OrderBy(p => p.Length).First();

            return wordShort;
        }
        public string CapitaliceWord(string sentence)
        {
            sentence.ToLower();
            if (string.IsNullOrEmpty(sentence))
                return sentence;

            // Convertir la primera letra a mayúscula y el resto a minúsculas
            return char.ToUpper(sentence[0]) + sentence.Substring(1);
        }
    }
}
