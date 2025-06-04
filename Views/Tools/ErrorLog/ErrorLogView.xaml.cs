using System.Windows;
using GestLog.ViewModels;

namespace GestLog.Views.Tools.ErrorLog
{
    /// <summary>
    /// Lógica de interacción para ErrorLogView.xaml
    /// </summary>
    public partial class ErrorLogView : Window
    {
        public ErrorLogView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Constructor que permite pasar un ViewModel específico ya configurado
        /// </summary>
        /// <param name="viewModel">ViewModel para esta vista</param>
        public ErrorLogView(ErrorLogViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        /// <summary>
        /// Muestra la ventana como un diálogo modal con el owner especificado
        /// </summary>
        /// <param name="owner">La ventana propietaria</param>
        public void ShowErrorLog(Window owner)
        {
            Owner = owner;
            ShowDialog();
        }
    }
}
