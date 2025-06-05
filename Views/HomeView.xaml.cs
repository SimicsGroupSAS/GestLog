using System.Windows;
using System.Windows.Controls;
using GestLog.Views.Tools;
using GestLog.Tests;

namespace GestLog.Views
{    /// <summary>
    /// Interaction logic for HomeView.xaml
    /// </summary>
    public partial class HomeView : System.Windows.Controls.UserControl
    {
        private MainWindow? _mainWindow;

        public HomeView()
        {
            InitializeComponent();
            _mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        }

        private void btnIrHerramientas_Click(object sender, RoutedEventArgs e)
        {
            var herramientasView = new HerramientasView();
            _mainWindow?.NavigateToView(herramientasView, "Herramientas");
        }        private void btnInfo_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show(
                "GestLog v1.0\n\n" +
                "Sistema modular de gestión integrada\n" +
                "Desarrollado con .NET 9 y WPF\n\n" +
                "Módulos integrados:\n" +
                "• DaaterProccesor - Procesamiento de datos Excel\n\n" +
                "Estado: ✅ Operativo",
                "Información del Sistema",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}
