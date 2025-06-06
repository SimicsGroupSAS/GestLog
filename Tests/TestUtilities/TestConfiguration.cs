using System;
using System.Threading.Tasks;
using GestLog.Tests.IntegrationTests;

namespace GestLog.Tests.TestUtilities;

/// <summary>
/// Programa para ejecutar tests de configuración desde línea de comandos
/// </summary>
public class TestConfiguration
{
    /// <summary>
    /// Método principal para ejecutar tests de configuración
    /// </summary>
    public static async Task RunAsync(string[] args)
    {
        Console.WriteLine("🧪 Ejecutando tests del sistema de configuración GestLog");
        Console.WriteLine("======================================================");
        
        try
        {
            var success = await ConfigurationSystemTest.RunTestsAsync();
            
            Console.WriteLine("\n======================================================");
            
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
