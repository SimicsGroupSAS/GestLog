using System;
using System.Globalization;
using System.Windows.Data;

namespace GestLog.Converters
{
    /// <summary>
    /// Convertidor que calcula el ancho de la barra de progreso basado en el porcentaje y ancho total
    /// </summary>
    public class ProgressToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && 
                values[0] is double progress && 
                values[1] is double totalWidth && 
                !double.IsNaN(totalWidth) && totalWidth > 0)
            {
                // Calcular ancho proporcional, asegurando que no exceda el ancho total
                var calculatedWidth = (progress / 100.0) * totalWidth;
                return Math.Max(0, Math.Min(calculatedWidth, totalWidth));
            }

            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
