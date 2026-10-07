using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VirtualKeyboard.Wpf.Converters;

class UppercaseTypographyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool uppercase = (bool)value;
        if (uppercase) return FontCapitals.AllSmallCaps;
        else return FontCapitals.Normal;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
