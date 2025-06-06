using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GestLog.Services.Core.UI
{
    /// <summary>
    /// Servicio para estimar el tiempo restante de operaciones basado en el progreso actual
    /// </summary>
    public class ProgressEstimationService
    {
        private readonly Stopwatch _stopwatch;
        private readonly Queue<TimeProgressPoint> _progressHistory;
        private readonly int _historySize;
        private double _lastProgress = 0;

        /// <summary>
        /// Constructor del servicio de estimación de tiempo
        /// </summary>
        /// <param name="historySize">Número de puntos de progreso a mantener para la estimación (predeterminado: 10)</param>
        public ProgressEstimationService(int historySize = 10)
        {
            _historySize = Math.Max(historySize, 5); // Mínimo 5 puntos para una estimación razonable
            _progressHistory = new Queue<TimeProgressPoint>(_historySize);
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        /// <summary>
        /// Registra un nuevo punto de progreso y actualiza la estimación
        /// </summary>
        /// <param name="progress">Porcentaje de progreso actual (0-100)</param>
        /// <returns>Estimación de tiempo restante o null si no hay suficientes datos</returns>
        public TimeSpan? UpdateProgress(double progress)
        {
            // No registrar si el progreso no ha cambiado significativamente (evitar ruido)
            if (Math.Abs(progress - _lastProgress) < 0.1) 
                return GetRemainingTime();
                
            _lastProgress = progress;
            var now = _stopwatch.Elapsed;
            
            // Añadir nuevo punto de progreso
            _progressHistory.Enqueue(new TimeProgressPoint(now, progress));
            
            // Mantener solo los últimos N puntos para cálculos más precisos
            if (_progressHistory.Count > _historySize)
                _progressHistory.Dequeue();
                
            return GetRemainingTime();
        }
        
        /// <summary>
        /// Obtiene la estimación actual del tiempo restante
        /// </summary>
        public TimeSpan? GetRemainingTime()
        {
            if (_progressHistory.Count < 3) // Necesitamos al menos algunos puntos para una estimación razonable
                return null;
                
            var points = new List<TimeProgressPoint>(_progressHistory);
            var lastPoint = points[points.Count - 1];
            
            // Si ya estamos al 100%, no queda tiempo restante
            if (lastPoint.Progress >= 99.9) 
                return TimeSpan.Zero;
                
            // Calcular velocidad promedio reciente (unidades de progreso por segundo)
            double speedSum = 0;
            int speedCount = 0;
            
            for (int i = 1; i < points.Count; i++)
            {
                var current = points[i];
                var previous = points[i - 1];
                
                var timeDiff = (current.Time - previous.Time).TotalSeconds;
                if (timeDiff > 0)
                {
                    var progressDiff = current.Progress - previous.Progress;
                    var speed = progressDiff / timeDiff; // Unidades de progreso por segundo
                    
                    if (speed > 0) // Solo considerar avances positivos
                    {
                        speedSum += speed;
                        speedCount++;
                    }
                }
            }
            
            // Si no tenemos suficientes datos de velocidad, no podemos estimar
            if (speedCount == 0)
                return null;
                
            double averageSpeed = speedSum / speedCount;
            if (averageSpeed <= 0)
                return null;
                
            // Calcular tiempo restante basado en la velocidad promedio
            var remainingProgress = 100 - lastPoint.Progress;
            var estimatedRemainingSeconds = remainingProgress / averageSpeed;
            
            // Limitar la estimación a un rango razonable
            estimatedRemainingSeconds = Math.Min(estimatedRemainingSeconds, 24 * 60 * 60); // Max 24 horas
            
            return TimeSpan.FromSeconds(estimatedRemainingSeconds);
        }
        
        /// <summary>
        /// Reinicia el servicio de estimación para una nueva operación
        /// </summary>
        public void Reset()
        {
            _progressHistory.Clear();
            _lastProgress = 0;
            _stopwatch.Restart();
        }
        
        /// <summary>
        /// Estructura interna para mantener puntos de tiempo-progreso
        /// </summary>
        private struct TimeProgressPoint
        {
            public TimeSpan Time { get; }
            public double Progress { get; }
            
            public TimeProgressPoint(TimeSpan time, double progress)
            {
                Time = time;
                Progress = progress;
            }
        }
    }
}
