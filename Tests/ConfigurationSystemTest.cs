using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using GestLog.Services;
using GestLog.Services.Configuration;
using GestLog.ViewModels.Configuration;

namespace GestLog.Tests;

/// <summary>
/// Test de validación para el sistema de configuración unificado
/// Verifica la funcionalidad end-to-end del sistema de configuración
/// </summary>
public class ConfigurationSystemTest
{
    private IConfigurationService? _configService;
    private ConfigurationViewModel? _viewModel;
    private IGestLogLogger? _logger;

    /// <summary>
    /// Ejecuta todas las pruebas del sistema de configuración
    /// </summary>
    public async Task<bool> RunAllTestsAsync()
    {
        try
        {
            // Inicializar servicios
            var serviceProvider = LoggingService.GetServiceProvider();
            _configService = serviceProvider.GetRequiredService<IConfigurationService>();
            _logger = serviceProvider.GetRequiredService<IGestLogLogger>();
            _viewModel = new ConfigurationViewModel(_configService, _logger);

            _logger.LogInformation("🧪 Iniciando tests del sistema de configuración");            // Ejecutar tests individuales
            var tests = new (string testName, Func<Task<bool>> testMethod)[]
            {
                ("Inicialización de servicios", TestServiceInitialization),
                ("Carga de configuración", TestConfigurationLoad),
                ("Guardado de configuración", TestConfigurationSave),
                ("Validación de configuración", TestConfigurationValidation),
                ("ViewModel funcional", TestViewModelFunctionality),
                ("Exportación/Importación", TestExportImport),
                ("Restauración por defecto", TestResetToDefaults)
            };

            int passed = 0;
            int failed = 0;

            foreach (var (testName, testMethod) in tests)
            {
                try
                {
                    _logger.LogInformation("🔄 Ejecutando test: {TestName}", testName);
                    var result = await testMethod();
                    
                    if (result)
                    {
                        _logger.LogInformation("✅ Test PASÓ: {TestName}", testName);
                        passed++;
                    }                    else
                    {
                        _logger.LogError(new InvalidOperationException($"Test falló: {testName}"), 
                            "❌ Test FALLÓ: {TestName}", testName);
                        failed++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "💥 Test EXPLOTÓ: {TestName}", testName);
                    failed++;
                }
            }

            var totalTests = passed + failed;
            var successRate = (double)passed / totalTests * 100;

            _logger.LogInformation("📊 Resultados de tests: {Passed}/{Total} pasaron ({SuccessRate:F1}%)", 
                passed, totalTests, successRate);

            return failed == 0;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "💥 Error crítico durante la ejecución de tests");
            return false;
        }
    }    /// <summary>
    /// Test 1: Verifica que los servicios se inicialicen correctamente
    /// </summary>
    private async Task<bool> TestServiceInitialization()
    {
        await Task.Delay(1); // Para hacer el método realmente async
        
        if (_configService == null)
        {
            _logger?.LogError(new InvalidOperationException("ConfigurationService no inicializado"), 
                "ConfigurationService no inicializado");
            return false;
        }

        if (_viewModel == null)
        {
            _logger?.LogError(new InvalidOperationException("ConfigurationViewModel no inicializado"), 
                "ConfigurationViewModel no inicializado");
            return false;
        }

        if (_logger == null)
        {
            Console.WriteLine("Logger no inicializado");
            return false;
        }

        // Verificar configuración actual existe
        var currentConfig = _configService.Current;
        if (currentConfig == null)
        {
            _logger.LogError(new InvalidOperationException("Configuración actual es nula"), 
                "Configuración actual es nula");
            return false;
        }

        _logger.LogDebug("✅ Servicios inicializados correctamente");
        return true;
    }

    /// <summary>
    /// Test 2: Verifica la carga de configuración
    /// </summary>
    private async Task<bool> TestConfigurationLoad()
    {
        await _configService!.LoadAsync();
        
        var config = _configService.Current;
          // Verificar que las secciones principales existen
        if (config.General == null)
        {
            _logger!.LogError(new InvalidOperationException("Sección General no cargada"), 
                "Sección General no cargada");
            return false;
        }

        if (config.UI == null)
        {
            _logger!.LogError(new InvalidOperationException("Sección UI no cargada"), 
                "Sección UI no cargada");
            return false;
        }

        if (config.Logging == null)
        {
            _logger!.LogError(new InvalidOperationException("Sección Logging no cargada"), 
                "Sección Logging no cargada");
            return false;
        }

        if (config.Performance == null)
        {
            _logger!.LogError(new InvalidOperationException("Sección Performance no cargada"), 
                "Sección Performance no cargada");
            return false;
        }

        if (config.Modules == null)
        {
            _logger!.LogError(new InvalidOperationException("Sección Modules no cargada"), 
                "Sección Modules no cargada");
            return false;
        }

        _logger!.LogDebug("✅ Configuración cargada con todas las secciones");
        return true;
    }

    /// <summary>
    /// Test 3: Verifica el guardado de configuración
    /// </summary>
    private async Task<bool> TestConfigurationSave()
    {
        // Modificar un valor
        var originalValue = _configService!.Current.General.ApplicationName;
        _configService.Current.General.ApplicationName = "Test GestLog";
        
        await _configService.SaveAsync();
        
        // Recargar y verificar
        await _configService.LoadAsync();
          if (_configService.Current.General.ApplicationName != "Test GestLog")
        {
            _logger!.LogError(new InvalidOperationException("Valor no se guardó correctamente"), 
                "Valor no se guardó correctamente");
            return false;
        }

        // Restaurar valor original
        _configService.Current.General.ApplicationName = originalValue;
        await _configService.SaveAsync();

        _logger!.LogDebug("✅ Guardado y carga funcionan correctamente");
        return true;
    }

    /// <summary>
    /// Test 4: Verifica la validación de configuración
    /// </summary>
    private async Task<bool> TestConfigurationValidation()
    {
        var errors = await _configService!.ValidateAsync();
        
        // Forzar un error de validación
        var originalFontSize = _configService.Current.UI.FontSize;
        _configService.Current.UI.FontSize = 200; // Valor inválido
        
        var errorsWithInvalidValue = await _configService.ValidateAsync();
          if (errorsWithInvalidValue.Count() == 0)
        {
            _logger!.LogError(new InvalidOperationException("Validación no detectó valor inválido"), 
                "Validación no detectó valor inválido");
            return false;
        }

        // Restaurar valor válido
        _configService.Current.UI.FontSize = originalFontSize;
        
        var errorsAfterFix = await _configService.ValidateAsync();
        
        _logger!.LogDebug("✅ Sistema de validación funciona correctamente");
        return true;
    }

    /// <summary>
    /// Test 5: Verifica la funcionalidad del ViewModel
    /// </summary>
    private async Task<bool> TestViewModelFunctionality()
    {
        // Cargar configuración en ViewModel
        await _viewModel!.LoadConfigurationCommand.ExecuteAsync(null);
          if (_viewModel.Configuration == null)
        {
            _logger!.LogError(new InvalidOperationException("ViewModel no cargó la configuración"), 
                "ViewModel no cargó la configuración");
            return false;
        }

        // Verificar secciones disponibles
        if (_viewModel.AvailableSections.Count == 0)
        {
            _logger!.LogError(new InvalidOperationException("ViewModel no tiene secciones disponibles"), 
                "ViewModel no tiene secciones disponibles");
            return false;
        }

        // Verificar cambio de sección
        _viewModel.ChangeSectionCommand.Execute("UI");
        
        if (_viewModel.SelectedSection != "UI")
        {
            _logger!.LogError(new InvalidOperationException("Cambio de sección no funcionó"), 
                "Cambio de sección no funcionó");
            return false;
        }

        _logger!.LogDebug("✅ ViewModel funciona correctamente");
        return true;
    }

    /// <summary>
    /// Test 6: Verifica exportación e importación
    /// </summary>
    private async Task<bool> TestExportImport()
    {
        var tempExportFile = Path.GetTempFileName() + ".json";
        
        try
        {
            // Exportar configuración actual
            await _configService!.ExportAsync(tempExportFile);
              if (!File.Exists(tempExportFile))
            {
                _logger!.LogError(new FileNotFoundException("Archivo de exportación no se creó"), 
                    "Archivo de exportación no se creó");
                return false;
            }

            // Modificar configuración
            var originalName = _configService.Current.General.ApplicationName;
            _configService.Current.General.ApplicationName = "Imported Test";
            
            // Importar configuración
            await _configService.ImportAsync(tempExportFile);
              if (_configService.Current.General.ApplicationName == "Imported Test")
            {
                _logger!.LogError(new InvalidOperationException("Importación no restauró el valor original"), 
                    "Importación no restauró el valor original");
                return false;
            }

            _logger!.LogDebug("✅ Exportación e importación funcionan correctamente");
            return true;
        }
        finally
        {
            if (File.Exists(tempExportFile))
            {
                File.Delete(tempExportFile);
            }
        }
    }

    /// <summary>
    /// Test 7: Verifica restauración a valores por defecto
    /// </summary>
    private async Task<bool> TestResetToDefaults()
    {
        // Modificar algunos valores
        _configService!.Current.General.ApplicationName = "Modified App";
        _configService.Current.UI.FontSize = 16;
          // Restaurar a valores por defecto
        await _configService.ResetToDefaultsAsync();
        
        // Verificar que los valores se restauraron
        if (_configService.Current.General.ApplicationName == "Modified App")
        {
            _logger!.LogError(new InvalidOperationException("Restauración no cambió el nombre de la aplicación"), 
                "Restauración no cambió el nombre de la aplicación");
            return false;
        }        _logger!.LogDebug("✅ Restauración a valores por defecto funciona correctamente");
#pragma warning disable CS0162
        return true;
#pragma warning restore CS0162
    }

    /// <summary>
    /// Método estático para ejecutar los tests desde cualquier lugar
    /// </summary>
    public static async Task<bool> RunTestsAsync()
    {
        var tester = new ConfigurationSystemTest();
        return await tester.RunAllTestsAsync();
    }
}
