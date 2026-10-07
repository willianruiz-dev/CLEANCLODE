namespace VirtualKeyboard.Wpf.Types;

/// <summary>
/// Tipo de teclado que se abre para un campo. Se asigna en XAML con la propiedad adjunta
/// <c>VKeyboard.InputKind</c>. El valor predeterminado mantiene el teclado alfabético de siempre.
/// </summary>
public enum KeyboardInputKind
{
    /// <summary>Teclado alfabético estándar.</summary>
    Default,

    /// <summary>Teclado numérico tipo clave (documento y celular).</summary>
    Numeric,

    /// <summary>Correo electrónico: símbolos de correo a la vista y dominios frecuentes para abreviar.</summary>
    Email
}
