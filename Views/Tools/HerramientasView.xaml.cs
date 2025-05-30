using System.Windows;
using System.Windows.Controls;
using GestLog.Views.Tools.DaaterProccesor;

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
}
