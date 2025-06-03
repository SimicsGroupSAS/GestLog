using System.Configuration;
using System.Data;
using System.Windows;
using GestLog.Services;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;

namespace GestLog;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IGestLogLogger? _logger;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Inicializar el sistema de logging y servicios
            LoggingService.InitializeServices();
            _logger = LoggingService.GetLogger();
            
            _logger.Logger.LogInformation("🚀 Aplicación GestLog iniciada");
            _logger.LogConfiguration("Version", "1.0.0");
            _logger.LogConfiguration("Environment", Environment.OSVersion.ToString());
            _logger.LogConfiguration("WorkingDirectory", Environment.CurrentDirectory);

            // Configurar manejo global de excepciones
            SetupGlobalExceptionHandling();
        }
        catch (Exception ex)
        {
            // Manejo de emergencia si falla la inicialización del logging
            MessageBox.Show($"Error crítico al inicializar la aplicación:\n{ex.Message}", 
                "Error de Inicialización", MessageBoxButton.OK, MessageBoxImage.Error);
            
            // Intentar logging de emergencia
            try
            {
                LoggingService.InitializeServices();
                _logger = LoggingService.GetLogger();
                _logger.LogUnhandledException(ex, "App.OnStartup");
            }
            catch
            {
                // Si ni siquiera el logging de emergencia funciona, salir
                Application.Current.Shutdown(1);
                return;
            }
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _logger?.Logger.LogInformation("🛑 Aplicación GestLog cerrándose");
            LoggingService.Shutdown();
        }
        catch (Exception ex)
        {
            // Log en consola como último recurso
            Console.WriteLine($"Error durante el cierre: {ex.Message}");
        }
        finally
        {
            base.OnExit(e);
        }
    }

    private void SetupGlobalExceptionHandling()
    {
        // Excepciones no manejadas en el hilo principal (UI)
        DispatcherUnhandledException += (sender, e) =>
        {
            _logger?.LogUnhandledException(e.Exception, "DispatcherUnhandledException");
            
            MessageBox.Show(
                $"Se produjo un error inesperado:\n\n{e.Exception.Message}\n\nLa aplicación intentará continuar. " +
                "Revise los logs para más detalles.",
                "Error no manejado", 
                MessageBoxButton.OK, 
                MessageBoxImage.Warning);
            
            e.Handled = true; // Permitir que la aplicación continúe
        };

        // Excepciones no manejadas en hilos secundarios
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            var exception = e.ExceptionObject as Exception;
            _logger?.LogUnhandledException(exception ?? new Exception("Unknown exception"), 
                "AppDomain.UnhandledException");
            
            if (e.IsTerminating)
            {
                _logger?.Logger.LogCritical("💥 La aplicación se está cerrando debido a una excepción no manejada");
                LoggingService.Shutdown();
            }
        };

        // Excepciones no observadas en Tasks
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            _logger?.LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
            e.SetObserved(); // Marcar como observada para evitar el cierre de la aplicación
        };
    }
}

