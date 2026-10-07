using System.ComponentModel;
using VirtualKeyboard.Wpf.Types;

namespace VirtualKeyboard.Wpf.ViewModels;

class VirtualKeyboardViewModel : INotifyPropertyChanged
{
    private string initialValue = string.Empty;

    private string _keyboardText;
    public string KeyboardText {
        get => _keyboardText;
        set
        {
            _keyboardText = value;
            NotifyPropertyChanged(nameof(KeyboardText));
        }
    }
    private KeyboardType _keyboardType;
    public KeyboardType KeyboardType {
        get => _keyboardType; 
        private set
        {
            _keyboardType = value;
            NotifyPropertyChanged(nameof(KeyboardType));
        }
    }

    /// <summary>Teclado con el que se abrió el host, para poder regresar a él.</summary>
    private readonly KeyboardType _initialKeyboardType;
    private bool _uppercase;
    public bool Uppercase
    {
        get => _uppercase;
        private set
        {
            _uppercase = value;
            NotifyPropertyChanged(nameof(Uppercase));
        }
    }
    private int _caretPosition;
    public int CaretPosition
    {
        get => _caretPosition;
        set
        {
            if (value < 0) _caretPosition = 0;
            else if (value > KeyboardText.Length) _caretPosition = KeyboardText.Length;
            else _caretPosition = value;
            NotifyPropertyChanged(nameof(CaretPosition));
        }
    }
    private string _selectedValue;
    public string SelectedValue
    {
        get => _selectedValue;
        set
        {
            _selectedValue = value;
            NotifyPropertyChanged(nameof(SelectedValue));
        }
    }
    public Command AddCharacter { get; }
    public Command ChangeCasing { get; }
    public Command RemoveCharacter { get; }
    public Command ChangeKeyboardType { get; }
    public Command Accept { get; }
    public Command Cancel { get; }
    public Command ClearAll { get; }

    public VirtualKeyboardViewModel(string initialValue) : this(initialValue, KeyboardType.Alphabet)
    {
    }

    public VirtualKeyboardViewModel(string initialValue, KeyboardType keyboardType)
    {
        this.initialValue = initialValue;
        _keyboardText = initialValue;
        _keyboardType = keyboardType;
        _initialKeyboardType = keyboardType;
        _uppercase = false;
        CaretPosition = _keyboardText.Length;

        AddCharacter = new Command(a =>
        {
            if (a is string character)
                if (character.Length == 1)
                {
                    if (Uppercase) character = character.ToUpper();
                    if (!string.IsNullOrEmpty(SelectedValue))
                    {
                        RemoveSubstring(SelectedValue);
                        KeyboardText = KeyboardText.Insert(CaretPosition, character);
                        CaretPosition++;
                        SelectedValue = "";
                    }
                    else
                    {
                        KeyboardText = KeyboardText.Insert(CaretPosition, character);
                        CaretPosition++;
                    }
                }
        });
        ChangeCasing = new Command(a => Uppercase = !Uppercase);
        RemoveCharacter = new Command(a =>
        {
            if(!string.IsNullOrEmpty(SelectedValue))
            {
                RemoveSubstring(SelectedValue);
            }
            else
            {
                var position = CaretPosition - 1;
                if (position >= 0)
                {
                    KeyboardText = KeyboardText.Remove(position, 1);
                    if (position < KeyboardText.Length) CaretPosition--;
                    else CaretPosition = KeyboardText.Length;
                }
            }
        });
        ChangeKeyboardType = new Command(a =>
        {
            // Vuelve al teclado con el que se abrió (alfabético o de correo), no siempre al alfabético.
            if (KeyboardType == KeyboardType.Special) KeyboardType = _initialKeyboardType;
            else KeyboardType = KeyboardType.Special;
        });
        Accept = new Command(a => VKeyboard.Close());
        Cancel = new Command(a => 
        {
            KeyboardText = this.initialValue;
            VKeyboard.Close();
        });
        ClearAll = new Command(a => 
        {
            KeyboardText = "";
            CaretPosition = KeyboardText.Length;
        });
    }

    private void RemoveSubstring(string substring)
    {
        var position = KeyboardText.IndexOf(substring);
        KeyboardText = KeyboardText.Remove(position, substring.Length);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void NotifyPropertyChanged(string prop)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
