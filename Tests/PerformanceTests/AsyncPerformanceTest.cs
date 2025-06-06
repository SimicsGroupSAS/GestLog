using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Modules.DaaterProccesor.Services;
using GestLog.Services;

namespace GestLog.Tests.PerformanceTests
{
    /// <summary>
    /// Clase para probar y demostrar las mejoras de rendimiento con async/await
    /// </summary>
    public class AsyncPerformanceTest
    {
        private readonly IExcelProcessingService _excelProcessingService;
        private readonly IExcelExportService _excelExportService;        public AsyncPerformanceTest()
        {
            _excelProcessingService = new ExcelProcessingService(
                new ResourceLoaderService(LoggingService.GetLogger<ResourceLoaderService>()),
                new DataConsolidationService(LoggingService.GetLogger<DataConsolidationService>()),
                new ExcelExportService(LoggingService.GetLogger<ExcelExportService>())
            );
            _excelExportService = new ExcelExportService(LoggingService.GetLogger<ExcelExportService>());
        }

        /// <summary>
        /// Demuestra el procesamiento asíncrono con cancelación
        /// </summary>
        public async Task<string> TestAsyncProcessingWithCancellationAsync(string folderPath)
        {
            var stopwatch = Stopwatch.StartNew();
            var cancellationTokenSource = new CancellationTokenSource();
            
            try
            {
                // Configurar cancelación después de 30 segundos (para demostración)
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(30));
                
                var progress = new Progress<double>(p =>
                {
                    Console.WriteLine($"Progreso: {p:F1}%");
                });

                Console.WriteLine("🚀 Iniciando procesamiento asíncrono...");
                
                // Procesamiento asíncrono
                var resultado = await _excelProcessingService.ProcesarArchivosExcelAsync(
                    folderPath, 
                    progress, 
                    cancellationTokenSource.Token
                );

                var outputPath = Path.Combine(folderPath, "Output", "Test_Consolidado_Async.xlsx");
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

                // Exportación asíncrona
                await _excelExportService.ExportarConsolidadoAsync(
                    resultado, 
                    outputPath, 
                    cancellationTokenSource.Token
                );

                stopwatch.Stop();
                
                return $"✅ Procesamiento completado en {stopwatch.Elapsed.TotalSeconds:F2} segundos. " +
                       $"Archivo guardado: {outputPath}";
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                return $"⚠️ Operación cancelada después de {stopwatch.Elapsed.TotalSeconds:F2} segundos";
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return $"❌ Error después de {stopwatch.Elapsed.TotalSeconds:F2} segundos: {ex.Message}";
            }
            finally
            {
                cancellationTokenSource.Dispose();
            }
        }

        /// <summary>
        /// Simula múltiples operaciones concurrentes
        /// </summary>
        public async Task<string> TestConcurrentOperationsAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Crear datos de prueba
                var testData = CreateTestDataTable();
                var outputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "GestLog_Test");
                Directory.CreateDirectory(outputDir);

                // Ejecutar múltiples exportaciones en paralelo
                var tasks = new Task[3];
                for (int i = 0; i < 3; i++)
                {
                    var fileName = Path.Combine(outputDir, $"Test_Export_{i + 1}.xlsx");
                    tasks[i] = _excelExportService.ExportarConsolidadoAsync(testData, fileName);
                }

                await Task.WhenAll(tasks);
                stopwatch.Stop();

                return $"✅ {tasks.Length} operaciones concurrentes completadas en {stopwatch.Elapsed.TotalSeconds:F2} segundos";
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return $"❌ Error en operaciones concurrentes: {ex.Message}";
            }
        }

        /// <summary>
        /// Crea una tabla de datos de prueba
        /// </summary>
        private DataTable CreateTestDataTable()
        {
            var dt = new DataTable();
            
            // Agregar columnas de prueba
            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("Fecha", typeof(DateTime));
            dt.Columns.Add("Producto", typeof(string));
            dt.Columns.Add("Cantidad", typeof(int));
            dt.Columns.Add("Precio", typeof(decimal));

            // Agregar datos de prueba
            for (int i = 1; i <= 1000; i++)
            {
                dt.Rows.Add(i, DateTime.Now.AddDays(-i), $"Producto {i}", i * 10, i * 1.5m);
            }

            return dt;
        }
    }
}
