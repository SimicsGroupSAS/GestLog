using System;
using System.Windows.Threading;

namespace GestLog.Services.Core.UI
{
    /// <summary>
    /// Servicio para ejecutar acciones en el hilo de la interfaz de usuario
    /// </summary>
    public static class DispatcherService
    {
        /// <summary>
        /// Ejecuta una acción en el hilo de la interfaz de usuario
        /// </summary>
        /// <param name="action">La acción a ejecutar</param>
        /// <param name="priority">La prioridad de la operación (por defecto es Normal)</param>
        public static void InvokeOnUIThread(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            
            if (dispatcher == null)
            {
                throw new InvalidOperationException("No se pudo obtener el Dispatcher. La aplicación podría estar cerrándose.");
            }
            
            if (dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                dispatcher.Invoke(action, priority);
            }
        }
        
        /// <summary>
        /// Ejecuta una acción en el hilo de la interfaz de usuario de forma asíncrona
        /// </summary>
        /// <param name="action">La acción a ejecutar</param>
        /// <param name="priority">La prioridad de la operación (por defecto es Normal)</param>
        public static void BeginInvokeOnUIThread(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            
            if (dispatcher == null)
            {
                throw new InvalidOperationException("No se pudo obtener el Dispatcher. La aplicación podría estar cerrándose.");
            }
            
            if (dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                dispatcher.BeginInvoke(action, priority);
            }
        }
    }
}
