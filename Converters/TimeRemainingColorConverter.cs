using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GestLog.Converters
{
    /// <summary>
    /// Conversor que cambia el color del texto de tiempo restante según el contexto
    /// </summary>
    public class TimeRemainingColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string timeText)
            {
                // Color naranja estándar para tiempo restante normal
                var defaultColor = Color.FromRgb(230, 126, 34); // #E67E22
                
                // Determinar el tipo de mensaje
                if (string.IsNullOrWhiteSpace(timeText))
                    return new SolidColorBrush(defaultColor);
                
                if (timeText.Contains("Completado"))
                    return new SolidColorBrush(Color.FromRgb(40, 167, 69)); // Verde #28A745
                
                if (timeText.Contains("Error"))
                    return new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Rojo #DC3545
                    
                if (timeText.Contains("Cancelado") || timeText.Contains("Cancelando"))
                    return new SolidColorBrush(Color.FromRgb(108, 117, 125)); // Gris #6C757D
                
                // Si es tiempo breve (menos de 10 segundos) usar rojo para indicar finalización inminente
                if (timeText.Contains("Tiempo restante: ") && timeText.Contains("s") && !timeText.Contains("m"))
                {
                    try
                    {
                        string secondsString = timeText.Split(':')[1].Trim().Split('s')[0].Trim();
                        if (int.TryParse(secondsString, out int seconds) && seconds < 10)
                        {
                            return new SolidColorBrush(Color.FromRgb(255, 87, 34)); // Naranja intenso #FF5722
                        }
                    }
                    catch { /* Si hay error de formato, usar color por defecto */ }
                }
                
                return new SolidColorBrush(defaultColor);
            }
            
            // Valor por defecto
            return new SolidColorBrush(Color.FromRgb(230, 126, 34)); // #E67E22
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
