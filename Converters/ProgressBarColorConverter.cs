using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GestLog.Converters
{
    /// <summary>
    /// Conversor que cambia el color de la barra de progreso según su valor
    /// </summary>
    public class ProgressBarColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double progress)
            {
                // Colores en diferentes etapas del progreso para una experiencia visual más rica
                if (progress < 25)
                    return new SolidColorBrush(Color.FromRgb(23, 162, 184)); // #17A2B8 - Info blue
                else if (progress < 50)
                    return new SolidColorBrush(Color.FromRgb(40, 167, 69));  // #28A745 - Success green
                else if (progress < 75)
                    return new SolidColorBrush(Color.FromRgb(0, 123, 255));   // #007BFF - Primary blue
                else if (progress < 99)
                    return new SolidColorBrush(Color.FromRgb(230, 126, 34));  // #E67E22 - Warning orange
                else
                    return new SolidColorBrush(Color.FromRgb(40, 167, 69));   // #28A745 - Success green at completion
            }
            
            // Valor predeterminado en caso de error
            return new SolidColorBrush(Color.FromRgb(40, 167, 69)); // #28A745 - Default green
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
