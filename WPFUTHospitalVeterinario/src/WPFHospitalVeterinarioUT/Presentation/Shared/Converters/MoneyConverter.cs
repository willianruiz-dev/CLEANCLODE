using System;
using System.Globalization;
using System.Windows.Data;

namespace Presentation.Shared.Converters
{
    /// <summary>
    /// Convierte un valor Decimal a formato monetario colombiano: $26.600
    /// Sin depender de la region de Windows (evita XDR)
    /// </summary>
    public class MoneyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "$0";

            if (value is decimal dec)
                return "$" + dec.ToString("N0");

            if (value is int intVal)
                return "$" + intVal.ToString("N0");

            if (value is double dbl)
                return "$" + dbl.ToString("N0");

            if (decimal.TryParse(value.ToString(), out decimal parsed))
                return "$" + parsed.ToString("N0");

            return "$0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
