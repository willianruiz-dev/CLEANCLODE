using System.Windows.Controls;
using VirtualKeyboard.Wpf.ViewModels;

namespace VirtualKeyboard.Wpf.Views;

/// <summary>
/// Teclado numérico tipo clave para los campos que solo admiten dígitos
/// (número de documento y número de celular). Reutiliza el teclado de la pantalla de pago
/// para conservar el mismo diseño y el mismo tamaño de tecla.
/// </summary>
partial class NumericView : UserControl
{
    public NumericView()
    {
        InitializeComponent();
        Keypad.KeyboardPressed += Keypad_KeyboardPressed;
    }

    private void Keypad_KeyboardPressed(object? sender, string keyPressed)
    {
        if (DataContext is not VirtualKeyboardViewModel viewModel || string.IsNullOrEmpty(keyPressed))
            return;

        switch (keyPressed)
        {
            case "Remove":
                viewModel.RemoveCharacter.Execute(null);
                break;
            case "Clear":
                viewModel.ClearAll.Execute(null);
                break;
            default:
                if (keyPressed.Length == 1)
                    viewModel.AddCharacter.Execute(keyPressed);
                break;
        }
    }
}
