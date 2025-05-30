using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using GestLog.ViewModelsMigrated;
using GestLog.Views.Tools.DaaterProccesor;

namespace GestLog.Views;

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
