using System.Windows.Controls;

namespace VirtualKeyboard.Wpf.Views;

/// <summary>
/// Teclado numérico: se usa en los campos que solo admiten dígitos
/// (número de documento y número de celular).
/// </summary>
partial class NumericView : UserControl
{
    public NumericView()
    {
        InitializeComponent();
    }
}
