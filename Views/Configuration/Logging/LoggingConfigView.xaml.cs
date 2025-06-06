using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace GestLog.Views.Configuration.Logging;

public partial class LoggingConfigView : System.Windows.Controls.UserControl
{
    public LoggingConfigView()
    {
        InitializeComponent();
    }

    private void BrowseLogDirectory_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Seleccionar Directorio de Logs"
        };

        if (dialog.ShowDialog() == true)
        {
            if (DataContext is GestLog.Models.Configuration.LoggingSettings settings)
            {
                settings.LogDirectory = dialog.FolderName;
            }
        }
    }
}
