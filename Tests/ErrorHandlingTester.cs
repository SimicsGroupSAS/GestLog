using System;
using System.Threading.Tasks;
using System.Windows;
using GestLog.Services;
using GestLog.Views;
using GestLog.Views.Tools.ErrorLog;

namespace GestLog.Tests
{
    /// <summary>
    /// Clase de prueba para demostrar y validar el sistema de manejo de errores
    /// </summary>
    public class ErrorHandlingTester
    {
        private readonly IErrorHandlingService _errorHandler;
        private readonly IGestLogLogger _logger;

        public ErrorHandlingTester()
        {
            _errorHandler = LoggingService.GetErrorHandler();
            _logger = LoggingService.GetLogger<ErrorHandlingTester>();
        }

        /// <summary>
        /// Ejecuta una serie de pruebas para validar el sistema de manejo de errores
        /// </summary>
        public async Task RunTestSuite(Window owner)
        {
            _logger.LogInformation("Iniciando pruebas del sistema de manejo de errores");

            try
            {
                // 1. Prueba de error sincrónico
                TestSyncError();

                // 2. Prueba de error asincrónico
                await TestAsyncError();

                // 3. Prueba de error con valor de retorno
                TestErrorWithReturn();

                // 4. Prueba de error no manejado (simulado - en realidad será manejado)
                SimulateUnhandledException();

                // 5. Mostrar la ventana de registro de errores
                ShowErrorLog(owner);                System.Windows.MessageBox.Show(
                    "Todas las pruebas de errores se completaron exitosamente.\n" +
                    "Se han generado varios registros de errores de prueba que puede ver en la ventana de registro de errores.",
                    "Pruebas Completadas",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "Error durante las pruebas del manejador de errores");
                
                System.Windows.MessageBox.Show(
                    $"Error al ejecutar las pruebas: {ex.Message}",
                    "Error en Pruebas",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Prueba 1: Manejo de error sincrónico
        /// </summary>
        private void TestSyncError()
        {
            _logger.LogInformation("Ejecutando prueba de error sincrónico");
            
            _errorHandler.HandleOperation(() =>
            {
                // Generar un error deliberadamente
                throw new InvalidOperationException("Este es un error de prueba sincrónico");
            }, "Prueba de error sincrónico");
        }

        /// <summary>
        /// Prueba 2: Manejo de error asincrónico
        /// </summary>
        private async Task TestAsyncError()
        {
            _logger.LogInformation("Ejecutando prueba de error asincrónico");
            
            await _errorHandler.HandleOperationAsync(async () =>
            {
                // Simular operación asíncrona
                await Task.Delay(500);
                
                // Generar un error deliberadamente
                throw new InvalidOperationException("Este es un error de prueba asincrónico");
            }, "Prueba de error asincrónico");
        }

        /// <summary>
        /// Prueba 3: Manejo de error con valor de retorno
        /// </summary>
        private void TestErrorWithReturn()
        {
            _logger.LogInformation("Ejecutando prueba de error con valor de retorno");
            
            var result = _errorHandler.HandleOperation<int>(() =>
            {
                // Generar un error deliberadamente
                return int.Parse("esto_no_es_un_número");
            }, "Prueba de error con valor de retorno", defaultValue: -1);
            
            _logger.LogInformation("Resultado de operación con error: {Result} (debería ser -1)", result);
        }

        /// <summary>
        /// Prueba 4: Simular un error no manejado (que será capturado por el handler global)
        /// </summary>
        private void SimulateUnhandledException()
        {
            _logger.LogInformation("Simulando un error no manejado");
            
            try
            {
                // Esta excepción será lanzada directamente
                throw new ApplicationException("Este es un error simulado no manejado");
            }
            catch (Exception ex)
            {
                // Pero en realidad lo manejamos directamente con el error handler
                _errorHandler.HandleException(ex, "Simulación de error no manejado");
            }
        }

        /// <summary>
        /// Prueba 5: Mostrar la ventana de registro de errores
        /// </summary>
        private void ShowErrorLog(Window owner)
        {
            _logger.LogInformation("Mostrando ventana de registro de errores");
            
            try
            {
                var errorLogView = new ErrorLogView();
                errorLogView.ShowErrorLog(owner);
            }
            catch (Exception ex)
            {
                _errorHandler.HandleException(ex, "Error al mostrar registro de errores");
            }
        }
    }
}
