using System.Windows;
using System.Windows.Controls;
using GestLog.Views.Tools.DaaterProccesor;
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
            errorLogView.ShowErrorLog(_mainWindow);
        }
        catch (System.Exception ex)
        {
            var errorHandler = Services.LoggingService.GetErrorHandler();
            errorHandler.HandleException(ex, "Mostrar registro de errores desde herramientas");
        }
    }
}
