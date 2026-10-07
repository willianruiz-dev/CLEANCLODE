using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using VirtualKeyboard.Wpf.Controls;
using VirtualKeyboard.Wpf.Types;
using VirtualKeyboard.Wpf.ViewModels;
using VirtualKeyboard.Wpf.Views;

namespace VirtualKeyboard.Wpf;

public static class VKeyboard
{
    private const string _keyboardValueName = "KeyboardValueContent";
    private const string _keyboardName = "KeyboardContent";

    private static Type _hostType = typeof(DefaultKeyboardHost);

    private static TaskCompletionSource<string> _tcs;
    public static Window _windowHost;

    /// <summary>
    /// Propiedad adjunta con el tipo de entrada del campo. Define el teclado que se abre
    /// y los caracteres aceptados. Se asigna desde XAML, por ejemplo
    /// <c>keyboard:VKeyboard.InputKind="Numeric"</c>.
    /// </summary>
    public static readonly DependencyProperty InputKindProperty = DependencyProperty.RegisterAttached(
        "InputKind",
        typeof(KeyboardInputKind),
        typeof(VKeyboard),
        new PropertyMetadata(KeyboardInputKind.Default));

    public static void SetInputKind(DependencyObject element, KeyboardInputKind value) =>
        element.SetValue(InputKindProperty, value);

    public static KeyboardInputKind GetInputKind(DependencyObject element) =>
        (KeyboardInputKind)element.GetValue(InputKindProperty);

    public static void Config(Type hostType)
    {
        if (hostType.IsSubclassOf(typeof(Window))) _hostType = hostType;
        else throw new ArgumentException();
    }

    public static void Listen<T>(Expression<Func<T, string>> property) where T: UIElement
    {
        EventManager.RegisterClassHandler(typeof(T), UIElement.PreviewMouseLeftButtonDownEvent, (RoutedEventHandler)(async (s, e) =>
        {
            if (s is AdvancedTextBox) return;
            var memberSelectorExpression = property.Body as MemberExpression;
            if (memberSelectorExpression == null) return;
            var prop = memberSelectorExpression.Member as PropertyInfo;
            if (prop == null) return;

            // El campo decide qué teclado se abre y qué caracteres acepta.
            var inputKind = s is DependencyObject dependencyObject
                ? GetInputKind(dependencyObject)
                : KeyboardInputKind.Default;

            var initValue = (string?)prop.GetValue(s) ?? string.Empty;
            var value = await OpenAsync(initValue, ToKeyboardType(inputKind));

            // Se descarta cualquier carácter que no corresponda al campo (documento y celular numéricos, correo sin espacios).
            prop.SetValue(s, Sanitize(inputKind, value), null);
        }));
    }

    public static Task<string> OpenAsync(string initialValue = "")
    {
        return OpenAsync(initialValue, KeyboardType.Alphabet);
    }

    internal static Task<string> OpenAsync(string initialValue, KeyboardType keyboardType)
    {
        if (_windowHost != null) throw new InvalidOperationException();

        _tcs = new TaskCompletionSource<string>();
        _windowHost = (Window)Activator.CreateInstance(_hostType);
        _windowHost.DataContext = new VirtualKeyboardViewModel(initialValue, keyboardType);
        ((ContentControl)_windowHost.FindName(_keyboardValueName)).Content = new KeyboardValueView();
        ((ContentControl)_windowHost.FindName(_keyboardName)).Content = new VirtualKeyboardView();
        void handler(object s, CancelEventArgs a)
        {
            var result = GetResult();
            ((Window)s).Closing -= handler;
            _windowHost = null;
            _tcs?.SetResult(result);
            _tcs = null;
        }

        _windowHost.Closing += handler;

        _windowHost.Owner = Application.Current.MainWindow;
        _windowHost.Show();
        return _tcs.Task;
    }

    public static void Close()
    {
        if (_windowHost == null) throw new InvalidOperationException();

        _windowHost.Close();
    }

    private static string GetResult()
    {
        var viewModel = (VirtualKeyboardViewModel)_windowHost.DataContext;
        return viewModel.KeyboardText;
    }

    private static KeyboardType ToKeyboardType(KeyboardInputKind inputKind) => inputKind switch
    {
        KeyboardInputKind.Numeric => KeyboardType.Numeric,
        KeyboardInputKind.Email => KeyboardType.Email,
        _ => KeyboardType.Alphabet
    };

    private static string Sanitize(KeyboardInputKind inputKind, string? value)
    {
        value ??= string.Empty;

        return inputKind switch
        {
            KeyboardInputKind.Numeric => new string(value.Where(char.IsDigit).ToArray()),
            KeyboardInputKind.Email => new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray()),
            _ => value
        };
    }
}
