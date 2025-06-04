using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GestLog.Converters
{
    /// <summary>
    /// Convierte un valor nulo a Collapsed y cualquier valor no nulo a Visible
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Convierte un valor de objeto a un valor de Visibility.
        /// </summary>
        /// <param name="value">El valor producido por la fuente de enlace.</param>
        /// <param name="targetType">El tipo de la propiedad de enlace de destino.</param>
        /// <param name="parameter">El parámetro de convertidor que se utilizará (opcional).</param>
        /// <param name="culture">La cultura que se utiliza en el convertidor.</param>
        /// <returns>Visibility.Visible si el valor no es nulo; de lo contrario, Visibility.Collapsed.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Si el parámetro es "invert", invierte la lógica
            bool invert = parameter is string paramStr && paramStr.ToLower() == "invert";
            bool isVisible = value != null;
            
            if (invert)
                isVisible = !isVisible;
                
            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// No implementado - conversión de vuelta no es necesaria para este caso de uso
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
