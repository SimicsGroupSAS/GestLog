using System.Windows.Controls;
using GestLog.Modules.GestionCartera.ViewModels;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Views.Tools.GestionCartera
{
    public partial class GestionCarteraView : System.Windows.Controls.UserControl
    {
        public GestionCarteraView()
        {
            InitializeComponent();
            
            // Usar inyección de dependencias para obtener el ViewModel
            var serviceProvider = LoggingService.GetServiceProvider();
            var viewModel = serviceProvider.GetRequiredService<DocumentGenerationViewModel>();
            DataContext = viewModel;
        }
    }
}
