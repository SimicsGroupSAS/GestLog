using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using GestLog.ViewModels.Configuration;
using GestLog.Services.Configuration;
using GestLog.Services;

namespace GestLog.Views.Configuration;

public partial class ConfigurationView : System.Windows.Controls.UserControl
{
    private ConfigurationViewModel? _viewModel;
    private System.Windows.Controls.Button? _activeButton;

    public ConfigurationView()
    {
        InitializeComponent();
        InitializeViewModel();
        LoadDefaultSection();
    }    private async void InitializeViewModel()
    {
        try
        {
            var configService = LoggingService.GetService<IConfigurationService>();
            if (configService != null)
            {
                var logger = LoggingService.GetLogger();
                _viewModel = new ConfigurationViewModel(configService, logger);
                DataContext = _viewModel;
                await _viewModel.LoadConfigurationCommand.ExecuteAsync(null);
            }
        }
        catch (System.Exception ex)
        {
            var errorHandler = LoggingService.GetErrorHandler();
            errorHandler.HandleException(ex, "Inicializar ViewModel de configuración");
        }
    }

    private void LoadDefaultSection()
    {
        // Cargar la sección General por defecto
        NavigateToSection("General");
        SetActiveButton(btnGeneral);
    }

    private void NavigationButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is string section)
        {
            NavigateToSection(section);
            SetActiveButton(button);
        }
    }

    private void NavigateToSection(string section)
    {
        System.Windows.Controls.UserControl? sectionView = section switch
        {
            "General" => new GeneralConfigView { DataContext = _viewModel?.Configuration?.General },
            "UI" => new UIConfigView { DataContext = _viewModel?.Configuration?.UI },
            "Logging" => new LoggingConfigView { DataContext = _viewModel?.Configuration?.Logging },
            "Performance" => new PerformanceConfigView { DataContext = _viewModel?.Configuration?.Performance },
            "Modules" => new ModulesConfigView { DataContext = _viewModel?.Configuration?.Modules },
            "DaaterProcessor" => new DaaterProcessorConfigView(),
            _ => null
        };

        if (sectionView != null)
        {
            ConfigContentPresenter.Content = sectionView;
        }
    }
    
    // Método para navegar directamente a la configuración de DaaterProcessor
    public void LoadDaaterProcessorConfigView()
    {
        NavigateToSection("DaaterProcessor");
        // No hay botón específico para DaaterProcessor en la barra lateral,
        // así que resaltamos "Modules" como opción activa
        SetActiveButton(btnModules);
    }

    private void SetActiveButton(System.Windows.Controls.Button button)
    {
        // Resetear todos los botones de navegación al estilo normal
        foreach (var child in NavigationPanel.Children)
        {
            if (child is System.Windows.Controls.Button navButton)
            {
                navButton.Style = (Style)FindResource("ConfigNavButtonStyle");
            }
        }

        // Establecer el nuevo botón activo con el estilo activo
        _activeButton = button;
        button.Style = (Style)FindResource("ConfigNavButtonActiveStyle");
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.SaveConfigurationCommand.ExecuteAsync(null);
            System.Windows.MessageBox.Show("Configuración guardada exitosamente.", "Configuración", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        var result = System.Windows.MessageBox.Show(
            "¿Está seguro de que desea restablecer toda la configuración a los valores predeterminados?",
            "Restablecer Configuración",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes && _viewModel != null)
        {
            await _viewModel.ResetToDefaultsCommand.ExecuteAsync(null);
            System.Windows.MessageBox.Show("Configuración restablecida a valores predeterminados.", "Configuración", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var saveDialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Exportar Configuración",
            Filter = "Archivos de Configuración (*.json)|*.json|Todos los archivos (*.*)|*.*",
            DefaultExt = "json",
            FileName = $"gestlog-config-{System.DateTime.Now:yyyyMMdd}.json"
        };

        if (saveDialog.ShowDialog() == true && _viewModel != null)
        {
            await _viewModel.ExportConfigurationCommand.ExecuteAsync(saveDialog.FileName);
            System.Windows.MessageBox.Show("Configuración exportada exitosamente.", "Exportar", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var openDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Importar Configuración",
            Filter = "Archivos de Configuración (*.json)|*.json|Todos los archivos (*.*)|*.*",
            DefaultExt = "json"
        };

        if (openDialog.ShowDialog() == true && _viewModel != null)
        {
            var result = System.Windows.MessageBox.Show(
                "¿Está seguro de que desea importar esta configuración? Se sobrescribirá la configuración actual.",
                "Importar Configuración",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await _viewModel.ImportConfigurationCommand.ExecuteAsync(openDialog.FileName);
                System.Windows.MessageBox.Show("Configuración importada exitosamente.", "Importar", 
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
