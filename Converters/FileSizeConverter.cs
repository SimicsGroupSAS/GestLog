using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace GestLog.Converters
{
    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string filePath && File.Exists(filePath))
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    var sizeInBytes = fileInfo.Length;
                    
                    string[] sizes = { "B", "KB", "MB", "GB" };
                    double len = sizeInBytes;
                    int order = 0;
                    
                    while (len >= 1024 && order < sizes.Length - 1)
                    {
                        order++;
                        len = len / 1024;
                    }
                    
                    return $"{len:0.##} {sizes[order]}";
                }
                catch
                {
                    return "N/A";
                }
            }
            return "N/A";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
