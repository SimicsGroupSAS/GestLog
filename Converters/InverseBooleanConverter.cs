using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GestLog.Converters;

/// <summary>
/// Convertidor que invierte un valor booleano y opcionalmente lo convierte a Visibility
/// </summary>
public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            var inverted = !boolValue;
            
            // Si se solicita conversión a Visibility
            if (targetType == typeof(Visibility) || parameter?.ToString() == "Visibility")
            {
                return inverted ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return inverted;
        }

        // Valor por defecto
        if (targetType == typeof(Visibility) || parameter?.ToString() == "Visibility")
        {
            return Visibility.Visible;
        }
        
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        
        if (value is Visibility visibility)
        {
            return visibility != Visibility.Visible; // Invertido porque es el ConvertBack
        }

        return false; // Valor por defecto si no es booleano ni Visibility
    }
}
