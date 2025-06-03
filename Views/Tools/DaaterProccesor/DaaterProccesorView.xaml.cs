using System.Windows;
using System.Windows.Controls;
using GestLog.Modules.DaaterProccesor.ViewModels;
using GestLog.Views.Tools.DaaterProccesor;

namespace GestLog.Views.Tools.DaaterProccesor;

public partial class DaaterProccesorView : UserControl
{
    public DaaterProccesorView()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void OnOpenFilteredDataViewClick(object sender, RoutedEventArgs e)
    {
        var window = new FilteredDataView();
        window.Show();
    }
}
