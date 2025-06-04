using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Services;

namespace GestLog.Tests
{
    /// <summary>
    /// Clase de prueba para verificar el rendimiento y fluidez del sistema de barra de progreso
    /// </summary>
    public class ProgressBarTest
    {
        private readonly SmoothProgressService _smoothProgressService;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private double _currentProgress = 0;
        private readonly TextWriter _output;

        public ProgressBarTest(TextWriter? output = null)
        {
            _output = output ?? Console.Out;
            _smoothProgressService = new SmoothProgressService(p => _currentProgress = p);
        }

        /// <summary>
        /// Ejecuta una prueba con un proceso simulado para verificar la fluidez del progreso
        /// </summary>
        /// <param name="totalDurationMs">Duración total de la simulación en milisegundos</param>
        /// <param name="updateIntervalMs">Intervalo entre actualizaciones en milisegundos</param>
        /// <param name="progressPattern">Patrón de progreso: "linear", "exponential", "stepwise" o "random"</param>
        /// <returns>Un informe de los resultados de la prueba</returns>
        public async Task<TestResult> RunSmoothProgressTest(
            int totalDurationMs = 10000,
            int updateIntervalMs = 100,
            string progressPattern = "linear")
        {
            // Resetear servicios
            _smoothProgressService.SetValueDirectly(0);
            _currentProgress = 0;
            
            _stopwatch.Restart();
            double lastReportedProgress = 0;
            var result = new TestResult();
            
            // Simular proceso con diferentes patrones de progreso
            int steps = totalDurationMs / updateIntervalMs;
            for (int i = 0; i < steps; i++)
            {
                // Calcular progreso según el patrón
                double progress;
                switch (progressPattern.ToLower())
                {
                    case "exponential":
                        // Progreso más rápido al principio, más lento al final
                        progress = 100 * (1 - Math.Exp(-5.0 * i / steps));
                        break;
                    case "stepwise":
                        // Progreso a saltos
                        progress = 100 * Math.Floor(4 * i / (double)steps) / 4;
                        break;
                    case "random":
                        // Progreso errático con tendencia ascendente
                        var rand = new Random();
                        double noise = rand.NextDouble() * 5 - 2.5; // Ruido entre -2.5% y +2.5%
                        progress = Math.Min(100, Math.Max(lastReportedProgress, 
                            100 * i / steps + noise));
                        break;
                    case "linear":
                    default:
                        // Progreso lineal
                        progress = 100 * i / (double)steps;
                        break;
                }
                
                // Limitar a rango válido
                progress = Math.Min(100, Math.Max(0, progress));
                lastReportedProgress = progress;
                
                // Reportar progreso al servicio de progreso suave
                _smoothProgressService.Report(progress);
                
                // Recopilar datos para el informe cada 10%
                if (Math.Floor(progress / 10) > Math.Floor(result.LastRecordedProgress / 10))
                {
                    result.RecordDataPoint(
                        progress, 
                        _currentProgress,
                        _stopwatch.Elapsed.TotalSeconds);
                    
                    _output.WriteLine($"Progreso reportado: {progress:F1}%, " +
                        $"Progreso suavizado: {_currentProgress:F1}%, " +
                        $"Tiempo transcurrido: {_stopwatch.Elapsed.TotalSeconds:F1}s");
                }
                
                // Esperar el intervalo de actualización
                await Task.Delay(updateIntervalMs);
            }
            
            _stopwatch.Stop();
            
            // Finalizar progreso
            _smoothProgressService.Report(100);
            result.TotalDurationSeconds = _stopwatch.Elapsed.TotalSeconds;
            result.TargetDurationSeconds = totalDurationMs / 1000.0;
            
            _output.WriteLine($"\nPrueba completada en {_stopwatch.Elapsed.TotalSeconds:F2}s");
            _output.WriteLine($"Progreso final suavizado: {_currentProgress:F1}%");
            
            return result;
        }
        
        /// <summary>
        /// Ejecuta pruebas con diferentes patrones y duración para simular casos reales
        /// </summary>
        public async Task RunAllTests()
        {
            _output.WriteLine("=== PRUEBAS DE BARRA DE PROGRESO SUAVE ===\n");
            
            _output.WriteLine("1. Prueba de proceso corto (5s) - Lineal");
            await RunSmoothProgressTest(5000, 50, "linear");
            
            _output.WriteLine("\n2. Prueba de proceso medio (20s) - Exponencial");
            await RunSmoothProgressTest(20000, 100, "exponential");
            
            _output.WriteLine("\n3. Prueba de proceso largo (45s) - Con saltos");
            await RunSmoothProgressTest(45000, 200, "stepwise");
            
            _output.WriteLine("\n4. Prueba de proceso con progreso aleatorio (15s)");
            await RunSmoothProgressTest(15000, 75, "random");
            
            _output.WriteLine("\n=== PRUEBAS COMPLETADAS ===");
        }
        
        /// <summary>
        /// Clase que contiene los resultados de la prueba
        /// </summary>
        public class TestResult
        {
            public double LastRecordedProgress { get; private set; } = 0;
            public double TotalDurationSeconds { get; set; }
            public double TargetDurationSeconds { get; set; }
            
            // Registros de progreso en diferentes puntos
            public readonly double[] ProgressPoints = new double[11]; // 0%, 10%, 20%... 100%
            public readonly double[] SmoothedProgressPoints = new double[11]; // Progreso suavizado en cada punto
            
            public void RecordDataPoint(
                double reportedProgress,
                double smoothedProgress,
                double elapsedSeconds)
            {
                LastRecordedProgress = reportedProgress;
                
                // Calcular índice del punto de progreso más cercano
                int index = (int)Math.Round(reportedProgress / 10);
                if (index < 0) index = 0;
                if (index > 10) index = 10;
                
                // Registrar progreso
                ProgressPoints[index] = reportedProgress;
                SmoothedProgressPoints[index] = smoothedProgress;
            }
        }
    }
}
