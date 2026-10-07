namespace VirtualKeyboard.Wpf.Types;

/// <summary>
/// Tipo de entrada esperada por un control. Permite abrir el teclado adecuado
/// (numérico, correo o alfabético) y descartar los caracteres que no correspondan al campo.
/// Se asigna en XAML con la propiedad adjunta <c>VKeyboard.InputKind</c>.
/// </summary>
public enum KeyboardInputKind
{
    /// <summary>Texto libre (comportamiento histórico del teclado alfabético).</summary>
    Default,

    /// <summary>Solo dígitos: número de documento y número de celular.</summary>
    Numeric,

    /// <summary>Correo electrónico: sin espacios y con símbolos de correo a la vista.</summary>
    Email
}
