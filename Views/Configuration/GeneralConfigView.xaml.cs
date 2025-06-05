using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace GestLog.Views.Configuration;

public partial class GeneralConfigView : System.Windows.Controls.UserControl
{
    public GeneralConfigView()
    {
        InitializeComponent();
    }

    private void BrowseWorkingDirectory_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Seleccionar Directorio de Trabajo"
        };

        if (dialog.ShowDialog() == true)
        {
            if (DataContext is GestLog.Models.Configuration.GeneralSettings settings)
            {
                settings.WorkingDirectory = dialog.FolderName;
            }
        }
    }
}
