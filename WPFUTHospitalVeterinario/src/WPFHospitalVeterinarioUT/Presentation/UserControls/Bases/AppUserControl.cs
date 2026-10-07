using Domain.UIServices;
using System.Windows.Controls;
using System.Windows.Threading;
using VirtualKeyboard.Wpf;

namespace UI.Bases
{
    public class AppUserControl : UserControl
    {
        protected Navigator _nav = Navigator.Instance;
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
