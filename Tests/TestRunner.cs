using System;
using System.Threading.Tasks;
using GestLog.Tests;
using GestLog.Services;

namespace GestLog.Testing;

/// <summary>
/// Programa de testing para validar el sistema de configuración
/// </summary>
public class TestRunner
{
    /// <summary>
    /// Método principal para ejecutar tests desde línea de comandos
    /// </summary>
    public static async Task RunAsync(string[] args)
    {
        Console.WriteLine("🧪 Sistema de Testing del Sistema de Configuración GestLog");
        Console.WriteLine("=========================================================");
        
        try
        {
            // Inicializar servicios antes de ejecutar tests
            Console.WriteLine("🔧 Inicializando servicios...");
            var serviceProvider = LoggingService.InitializeServices();
            
            Console.WriteLine("🚀 Ejecutando tests del sistema de configuración...");
            Console.WriteLine();
            
            var success = await ConfigurationSystemTest.RunTestsAsync();
            
            Console.WriteLine();
            Console.WriteLine("=========================================================");
            
            if (success)
            {
                Console.WriteLine("✅ TODOS LOS TESTS PASARON - Sistema de configuración funcional");
                Environment.Exit(0);
            }
            else
            {
                Console.WriteLine("❌ ALGUNOS TESTS FALLARON - Revisar logs para detalles");
                Environment.Exit(1);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 Error crítico durante testing: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Environment.Exit(2);
        }
    }
}
