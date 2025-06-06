using System;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Modules.DaaterProccesor.ViewModels;
using GestLog.Modules.DaaterProccesor.Services;

namespace GestLog.Tests.Demos
{
    /// <summary>
    /// Prueba para verificar que el botón de cancelación funciona correctamente
    /// </summary>
    public class CancelButtonTest
    {        public static Task TestCancelFunctionality()
        {
            Console.WriteLine("=== PRUEBA DE FUNCIONALIDAD DEL BOTÓN DE CANCELACIÓN ===");
            
            var viewModel = new MainViewModel();
            
            // Verificar estado inicial
            Console.WriteLine($"Estado inicial:");
            Console.WriteLine($"  IsProcessing: {viewModel.IsProcessing}");
            Console.WriteLine($"  CanProcessExcelFiles: {viewModel.ProcessExcelFilesCommand.CanExecute(null)}");
            Console.WriteLine($"  CanCancelProcessing: {viewModel.CancelProcessingCommand.CanExecute(null)}");
            Console.WriteLine($"  StatusMessage: {viewModel.StatusMessage}");
            
            // Simular el inicio del procesamiento
            Console.WriteLine("\n--- Simulando inicio de procesamiento ---");
            viewModel.IsProcessing = true;
            viewModel.StatusMessage = "Procesando archivos...";
            
            // Verificar que los comandos cambiaron
            Console.WriteLine($"Durante procesamiento:");
            Console.WriteLine($"  IsProcessing: {viewModel.IsProcessing}");
            Console.WriteLine($"  CanProcessExcelFiles: {viewModel.ProcessExcelFilesCommand.CanExecute(null)}");
            Console.WriteLine($"  CanCancelProcessing: {viewModel.CancelProcessingCommand.CanExecute(null)}");
            Console.WriteLine($"  StatusMessage: {viewModel.StatusMessage}");
            
            // Simular cancelación
            Console.WriteLine("\n--- Ejecutando comando de cancelación ---");
            if (viewModel.CancelProcessingCommand.CanExecute(null))
            {
                viewModel.CancelProcessingCommand.Execute(null);
                Console.WriteLine($"Comando de cancelación ejecutado.");
                Console.WriteLine($"  StatusMessage después de cancelar: {viewModel.StatusMessage}");
            }
            else
            {
                Console.WriteLine("ERROR: El comando de cancelación no se puede ejecutar.");
            }
            
            // Simular finalización
            Console.WriteLine("\n--- Simulando finalización ---");
            viewModel.IsProcessing = false;
            viewModel.StatusMessage = "Listo para procesar archivos.";
            
            Console.WriteLine($"Estado final:");
            Console.WriteLine($"  IsProcessing: {viewModel.IsProcessing}");
            Console.WriteLine($"  CanProcessExcelFiles: {viewModel.ProcessExcelFilesCommand.CanExecute(null)}");
            Console.WriteLine($"  CanCancelProcessing: {viewModel.CancelProcessingCommand.CanExecute(null)}");
            Console.WriteLine($"  StatusMessage: {viewModel.StatusMessage}");
            
            Console.WriteLine("\n=== PRUEBA COMPLETADA ===");
            
            return Task.CompletedTask;
        }
    }
}
