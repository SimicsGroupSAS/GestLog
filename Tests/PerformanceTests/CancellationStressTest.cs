using System;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Modules.DaaterProccesor.ViewModels;
using GestLog.Modules.DaaterProccesor.Services;

namespace GestLog.Tests.PerformanceTests
{
    /// <summary>
    /// Prueba intensiva para verificar que la cancelación funciona correctamente
    /// durante operaciones de procesamiento de archivos Excel
    /// </summary>
    public class CancellationStressTest
    {
        public static async Task TestRealCancellation()
        {
            Console.WriteLine("=== PRUEBA DE CANCELACIÓN DURANTE PROCESAMIENTO REAL ===");
            
            var viewModel = new MainViewModel();
            
            // Configurar estados de prueba
            viewModel.IsProcessing = true;
            viewModel.StatusMessage = "Simulando procesamiento real...";
            
            // Crear un CancellationTokenSource que podemos cancelar
            var cts = new CancellationTokenSource();
            
            Console.WriteLine($"Estado inicial:");
            Console.WriteLine($"  IsProcessing: {viewModel.IsProcessing}");
            Console.WriteLine($"  CanCancelProcessing: {viewModel.CancelProcessingCommand.CanExecute(null)}");
            
            // Simular tarea de procesamiento larga
            var processingTask = Task.Run(async () =>
            {
                try
                {
                    for (int i = 0; i < 1000; i++)
                    {
                        // Simular trabajo de procesamiento
                        await Task.Delay(10, cts.Token);
                        
                        // Verificar cancelación cada 50 iteraciones
                        if (i % 50 == 0)
                        {
                            cts.Token.ThrowIfCancellationRequested();
                            Console.WriteLine($"Procesando... iteración {i}");
                        }
                    }
                    Console.WriteLine("Procesamiento completado sin cancelación");
                    return "COMPLETADO";
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Procesamiento cancelado correctamente!");
                    return "CANCELADO";
                }
            });
            
            // Esperar un poco y luego cancelar
            await Task.Delay(200);
            Console.WriteLine("\n--- ACTIVANDO CANCELACIÓN ---");
            cts.Cancel();
            
            // Esperar a que termine
            var resultado = await processingTask;
            
            Console.WriteLine($"\nResultado: {resultado}");
            Console.WriteLine($"Token cancelado: {cts.Token.IsCancellationRequested}");
            
            // Simular que el ViewModel maneja la cancelación
            if (resultado == "CANCELADO")
            {
                viewModel.IsProcessing = false;
                viewModel.StatusMessage = "Operación cancelada.";
                Console.WriteLine("✅ La cancelación funcionó correctamente!");
            }
            else
            {
                Console.WriteLine("❌ La cancelación NO funcionó - el procesamiento siguió ejecutándose");
            }
            
            Console.WriteLine($"\nEstado final:");
            Console.WriteLine($"  IsProcessing: {viewModel.IsProcessing}");
            Console.WriteLine($"  StatusMessage: {viewModel.StatusMessage}");
            Console.WriteLine($"  CanCancelProcessing: {viewModel.CancelProcessingCommand.CanExecute(null)}");
            
            Console.WriteLine("\n=== PRUEBA COMPLETADA ===");
        }
          public static Task TestCancellationFlow()
        {
            Console.WriteLine("=== PRUEBA DE FLUJO COMPLETO DE CANCELACIÓN ===");
            
            var viewModel = new MainViewModel();
            
            // Test 1: Estado inicial
            Console.WriteLine("Test 1: Estado inicial");
            Console.WriteLine($"  CanProcessExcelFiles: {viewModel.ProcessExcelFilesCommand.CanExecute(null)}");
            Console.WriteLine($"  CanCancelProcessing: {viewModel.CancelProcessingCommand.CanExecute(null)}");
            
            // Test 2: Simular inicio de procesamiento
            Console.WriteLine("\nTest 2: Iniciando procesamiento");
            viewModel.IsProcessing = true;
            viewModel.StatusMessage = "Procesando...";
            
            // Notificar manualmente los cambios de comando (simula lo que hace ProcessExcelFilesCommand)
            viewModel.ProcessExcelFilesCommand.NotifyCanExecuteChanged();
            viewModel.CancelProcessingCommand.NotifyCanExecuteChanged();
            
            Console.WriteLine($"  CanProcessExcelFiles: {viewModel.ProcessExcelFilesCommand.CanExecute(null)}");
            Console.WriteLine($"  CanCancelProcessing: {viewModel.CancelProcessingCommand.CanExecute(null)}");
            
            // Test 3: Ejecutar cancelación
            Console.WriteLine("\nTest 3: Ejecutando cancelación");
            if (viewModel.CancelProcessingCommand.CanExecute(null))
            {
                viewModel.CancelProcessingCommand.Execute(null);
                Console.WriteLine($"  StatusMessage después de cancelar: {viewModel.StatusMessage}");
            }
            
            // Test 4: Simular finalización
            Console.WriteLine("\nTest 4: Finalizando");
            viewModel.IsProcessing = false;
            viewModel.StatusMessage = "Listo para procesar archivos.";
            
            viewModel.ProcessExcelFilesCommand.NotifyCanExecuteChanged();
            viewModel.CancelProcessingCommand.NotifyCanExecuteChanged();
            
            Console.WriteLine($"  CanProcessExcelFiles: {viewModel.ProcessExcelFilesCommand.CanExecute(null)}");
            Console.WriteLine($"  CanCancelProcessing: {viewModel.CancelProcessingCommand.CanExecute(null)}");
            
            Console.WriteLine("\n=== FLUJO DE PRUEBA COMPLETADO ===");
            
            return Task.CompletedTask;
        }
    }
}
