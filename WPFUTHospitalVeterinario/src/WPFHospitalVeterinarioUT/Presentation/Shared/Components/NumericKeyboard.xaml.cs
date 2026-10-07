using System.Windows.Controls;

namespace UI.Components
{
    /// <summary>
    /// Lógica de interacción para Teclado.xaml
    /// </summary>
    public partial class NumericKeyboard : UserControl
    {


        public event EventHandler<string> KeyboardPressed;
        public NumericKeyboard()
        {
            InitializeComponent();
        }

        private void Keyboard_MouseDown(object sender, EventArgs e)
        {
            Border key = (Border)sender;
            string tag = key.Tag.ToString() ?? "";
            KeyboardPressed?.Invoke(this, tag);
        }

      
    }
}
