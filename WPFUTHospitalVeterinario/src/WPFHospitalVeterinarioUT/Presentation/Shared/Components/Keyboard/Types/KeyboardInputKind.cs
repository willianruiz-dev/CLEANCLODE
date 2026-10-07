namespace VirtualKeyboard.Wpf.Types;

/// <summary>
/// Tipo de teclado que se abre para un campo. Se asigna en XAML con la propiedad adjunta
/// <c>VKeyboard.InputKind</c>. El valor predeterminado mantiene el teclado de siempre.
/// </summary>
public enum KeyboardInputKind
{
    /// <summary>Teclado alfabético estándar: es el que usan todos los campos salvo el correo.</summary>
    Default,

    /// <summary>Correo electrónico: símbolos de correo a la vista y dominios frecuentes para abreviar.</summary>
    Email
}
