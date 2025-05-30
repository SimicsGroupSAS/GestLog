using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using GestLog.Modules.DaaterProccesor.ViewModels;

namespace GestLog.Views.Tools.DaaterProccesor;

public partial class DaaterProccesorView : UserControl
{
    public DaaterProccesorView()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private async void OnProcessExcelFilesClick(object sender, RoutedEventArgs e)
    {
        var viewModel = (MainViewModel)DataContext;
        await viewModel.ProcessExcelFilesAsync();
    }    private void OnOpenFilteredDataViewClick(object sender, RoutedEventArgs e)
    {
        var window = new FilteredDataView();
        window.Show();
    }
}
