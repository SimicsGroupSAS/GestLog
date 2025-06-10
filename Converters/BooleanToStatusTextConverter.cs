using System;
using System.Globalization;
using System.Windows.Data;

namespace GestLog.Converters;

/// <summary>
/// Convertidor que transforma un valor booleano en texto de estado
/// </summary>
public class BooleanToStatusTextConverter : IValueConverter
{
    /// <summary>
    /// Texto para el valor true
    /// </summary>
    public string TrueText { get; set; } = "Configurado";

    /// <summary>
    /// Texto para el valor false
    /// </summary>
    public string FalseText { get; set; } = "No configurado";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? TrueText : FalseText;
        }

        return FalseText;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("BooleanToStatusTextConverter no admite conversión bidireccional.");
    }
}
