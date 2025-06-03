using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using GestLog.Views;
using GestLog.Services;
using Microsoft.Extensions.Logging;

namespace GestLog;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly Stack<(UserControl view, string title)> _navigationStack;
    private UserControl? _currentView;
    private readonly IGestLogLogger _logger;

    public MainWindow()
    {
        try
        {
            InitializeComponent();
            _navigationStack = new Stack<(UserControl, string)>();
            
            // Obtener logger del servicio de logging
            _logger = LoggingService.GetLogger<MainWindow>();
            
            _logger.LogApplicationStarted("MainWindow inicializada correctamente");
            
            // Cargar la vista Home por defecto
            LoadHomeView();
        }
        catch (System.Exception ex)
        {
            // Fallback en caso de que el logger no esté disponible
            var fallbackLogger = LoggingService.GetLogger<MainWindow>();
            fallbackLogger.LogError(ex, "Error al inicializar MainWindow");
            throw;
        }
    }

    private void LoadHomeView()
    {
        try
        {
            _logger.LogUserInteraction("🏠", "LoadHomeView", "Cargando vista principal");
            
            using var scope = _logger.BeginOperationScope("LoadHomeView");
            
            var homeView = new HomeView();
            contentPanel.Content = homeView;
            _currentView = homeView;
            txtCurrentView.Text = "Home";
            btnBack.Visibility = Visibility.Collapsed;
            _navigationStack.Clear();
            
            _logger.LogUserInteraction("✅", "LoadHomeView", "Vista Home cargada exitosamente");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al cargar la vista Home");
            throw;
        }
    }

    public void NavigateToView(UserControl view, string title)
    {
        try
        {
            _logger.LogUserInteraction("🧭", "NavigateToView", "Navegando a vista: {ViewTitle}", title);
            
            using var scope = _logger.BeginOperationScope("NavigateToView", new { ViewTitle = title });
            
            // Guardar la vista actual en el stack
            if (_currentView != null)
            {
                _navigationStack.Push((_currentView, txtCurrentView.Text));
                _logger.LogDebug("Vista actual guardada en stack: {PreviousView}", txtCurrentView.Text);
            }

            // Navegar a la nueva vista
            contentPanel.Content = view;
            _currentView = view;
            txtCurrentView.Text = title;
            btnBack.Visibility = Visibility.Visible;
            
            _logger.LogUserInteraction("✅", "NavigateToView", "Navegación completada a: {ViewTitle}", title);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al navegar a la vista: {ViewTitle}", title);
            throw;
        }
    }

    private void btnHome_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogUserInteraction("🏠", "btnHome_Click", "Usuario hizo clic en botón Home");
            LoadHomeView();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al procesar clic en botón Home");
        }
    }

    private void btnBack_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogUserInteraction("⬅️", "btnBack_Click", "Usuario hizo clic en botón Regresar");
            
            using var scope = _logger.BeginOperationScope("BackNavigation");
            
            if (_navigationStack.Count > 0)
            {
                var (previousView, previousTitle) = _navigationStack.Pop();
                contentPanel.Content = previousView;
                _currentView = previousView;
                txtCurrentView.Text = previousTitle;

                _logger.LogUserInteraction("📍", "btnBack_Click", "Regresando a vista: {PreviousTitle}", previousTitle);

                // Si no hay más vistas en el stack, ocultar el botón Back
                if (_navigationStack.Count == 0)
                {
                    btnBack.Visibility = Visibility.Collapsed;
                    _logger.LogDebug("Stack de navegación vacío, ocultando botón Back");
                }
            }
            else
            {
                // Si no hay stack, ir a Home
                _logger.LogDebug("Stack de navegación vacío, cargando vista Home");
                LoadHomeView();
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al procesar navegación hacia atrás");
        }
    }

    // Método para compatibilidad con código existente
    public void SetContent(UserControl control)
    {
        try
        {
            var controlType = control?.GetType().Name ?? "Unknown";
            _logger.LogUserInteraction("🔄", "SetContent", "Estableciendo contenido: {ControlType}", controlType);
            
            contentPanel.Content = control;
            _currentView = control;
            
            _logger.LogDebug("Contenido establecido exitosamente: {ControlType}", controlType);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al establecer contenido");
            throw;
        }
    }

    protected override void OnClosed(System.EventArgs e)
    {
        try
        {
            _logger.LogApplicationStarted("MainWindow cerrada por el usuario");
            base.OnClosed(e);
        }
        catch (System.Exception ex)
        {
            // En caso de error al cerrar, registrar pero no lanzar excepción
            var fallbackLogger = LoggingService.GetLogger<MainWindow>();
            fallbackLogger.LogError(ex, "Error al cerrar MainWindow");
        }
    }
}