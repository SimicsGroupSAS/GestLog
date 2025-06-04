using System.Windows;
using System.Windows.Controls;
using GestLog.Views.Tools.DaaterProccesor;
using GestLog.Views.Tools.ErrorLog;
using GestLog.Views;

namespace GestLog.Views.Tools;

public partial class HerramientasView : UserControl
{
    private MainWindow? _mainWindow;

    public HerramientasView()
    {
        InitializeComponent();
        _mainWindow = Application.Current.MainWindow as MainWindow;
    }

    private void BtnDaaterProccesor_Click(object sender, RoutedEventArgs e)
    {
        var daaterProccesorView = new DaaterProccesorView();
        _mainWindow?.NavigateToView(daaterProccesorView, "DaaterProccesor");
    }

    /// <summary>
    /// Muestra la ventana del registro de errores
    /// </summary>
    private void btnErrorLog_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var errorLogView = new ErrorLogView();
            
            // Verificar que _mainWindow no sea null antes de pasarlo como parámetro
            if (_mainWindow != null)
            {
                errorLogView.ShowErrorLog(_mainWindow);
            }
            else
            {
                // Usar la ventana actual si _mainWindow es null
                var currentWindow = Window.GetWindow(this);
                if (currentWindow != null)
                {
                    errorLogView.ShowErrorLog(currentWindow);
                }
                else
                {
                    // Si no hay ventana disponible, mostrar sin propietario
                    MessageBox.Show("No se pudo obtener una ventana propietaria para el visor de errores.",
                        "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                    errorLogView.Show();
                }
            }
        }
        catch (System.Exception ex)
        {
            var errorHandler = Services.LoggingService.GetErrorHandler();
            errorHandler.HandleException(ex, "Mostrar registro de errores desde herramientas");
        }
    }
}
