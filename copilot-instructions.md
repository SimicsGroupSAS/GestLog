````instructions
# Instrucciones de GitHub Copilot para GestLog

## 🎯 Contexto del Proyecto

GestLog es una aplicación WPF modular para gestión empresarial integral con arquitectura modular y separación de responsabilidades.

**Idioma**: Código en inglés, UI y mensajes en español (es-CO).

## 🏗️ Arquitectura Fundamental

### Patrón MVVM Estricto
```csharp
// ✅ ViewModels con CommunityToolkit.Mvvm
public partial class DocumentGenerationViewModel : ObservableObject
{
    private readonly IPdfGeneratorService _pdfService;
    private readonly IGestLogLogger _logger;
    
    [ObservableProperty]
    private string _selectedFilePath;
    
    [RelayCommand]
    private async Task GenerateDocumentsAsync(CancellationToken cancellationToken)
    {
        // Implementación con manejo de errores y logging
    }
}

// ❌ NO lógica en Code-Behind
```

### Inyección de Dependencias
```csharp
// ✅ Registro en App.xaml.cs
ServiceLocator.RegisterSingleton<IGestLogLogger, GestLogLogger>();
ServiceLocator.RegisterSingleton<IPdfGeneratorService, PdfGeneratorService>();
ServiceLocator.RegisterTransient<DocumentGenerationViewModel>();

// ✅ Constructor injection
public PdfGeneratorService(IGestLogLogger logger, IConfigurationService config)
{
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _config = config ?? throw new ArgumentNullException(nameof(config));
}
```

## 🎯 Principio de Responsabilidad Única (SRP)

**Cada clase debe tener una sola responsabilidad y una sola razón para cambiar.**

### Señales de Violación SRP
- Múltiples responsabilidades en una clase
- Comentarios que indican secciones diferentes (// Email, // PDF, etc.)
- Dificultad para nombrar la clase específicamente
- Métodos que manejan conceptos diferentes

### Refactorización SRP
```csharp
// ❌ Violación
public class DocumentGenerationViewModel
{
    // PDF Generation + Email Sending + SMTP Config
}

// ✅ SRP Aplicado
public class PdfGenerationViewModel { /* Solo PDF */ }
public class AutomaticEmailViewModel { /* Solo Email */ }
public class SmtpConfigurationViewModel { /* Solo SMTP */ }
public class MainDocumentGenerationViewModel { /* Orquestador */ }
```

## 🔄 Programación Asíncrona

```csharp
// ✅ SIEMPRE async/await para I/O
public async Task<List<GeneratedPdfInfo>> GenerateDocumentsAsync(
    string excelPath, 
    CancellationToken cancellationToken = default)
{
    try
    {
        _logger.LogInformation("Iniciando generación desde {ExcelPath}", excelPath);
        
        if (string.IsNullOrWhiteSpace(excelPath))
            throw new ArgumentException("Ruta requerida", nameof(excelPath));
            
        var companies = await LoadCompaniesFromExcelAsync(excelPath, cancellationToken);
        
        // Procesamiento con progreso y cancelación
        for (int i = 0; i < companies.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await GenerateDocumentForClientAsync(companies[i], cancellationToken);
            results.Add(result);
        }
        
        return results;
    }
    catch (OperationCanceledException)
    {
        _logger.LogWarning("Operación cancelada");
        throw;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error en generación");
        throw;
    }
}
```

## 📋 Logging y Manejo de Errores

```csharp
// ✅ Logging estructurado
_logger.LogInformation("PDF generado para {CompanyName}", company.Name);
_logger.LogError(ex, "Error generando PDF para {CompanyName}", company.Name);

// ✅ Excepciones específicas
public class PdfGenerationException : GestLogException
{
    public string CompanyName { get; }
    public PdfGenerationException(string message, string companyName) 
        : base(message, "PDF_GENERATION")
    {
        CompanyName = companyName;
    }
}

// ✅ Manejo en ViewModels
try
{
    var result = await _pdfService.GenerateDocumentsAsync(filePath, cancellationToken);
    await ShowSuccessMessageAsync($"Generados {result.Count} documentos");
}
catch (PdfGenerationException ex)
{
    await ShowErrorMessageAsync("Error de Generación", ex.Message);
}
```

## 🎯 Tecnologías Principales

- **.NET 9.0 + WPF**
- **CommunityToolkit.Mvvm** - `[ObservableProperty]`, `[RelayCommand]`
- **ClosedXML** - Excel
- **iText7** - PDF
- **IGestLogLogger** - Logging obligatorio

## 🚫 Anti-Patrones

```csharp
// ❌ NO hacer
public void ProcessData() { File.ReadAllText(path); } // Síncrono
catch (Exception ex) { Console.WriteLine("Error"); } // Log genérico
public string FilePath { get; set; } // Sin validación
private void Button_Click() { LoadData(); } // Lógica en code-behind
```

## 📁 Estructura de Módulos

```
Modules/[NombreModulo]/
├── ViewModels/          # Una responsabilidad UI por ViewModel
├── Services/            # Una responsabilidad de negocio por Service
├── Models/              # DTOs y entidades
└── Interfaces/          # Contratos
```

## 💡 Mensajes de Usuario

**Todos los mensajes al usuario en español:**
```csharp
// ✅ UI en español
ErrorMessage = "El archivo seleccionado no existe";
[Required(ErrorMessage = "Debe seleccionar un archivo Excel")]
MessageBox.Show("Operación completada exitosamente", "Éxito");

// ✅ Logs técnicos pueden ser en inglés
_logger.LogDebug("Processing Excel file: {FilePath}", filePath);
```

---

## ⚡ Reglas Rápidas

1. **SRP**: Una responsabilidad por clase
2. **Async**: Siempre para I/O
3. **DI**: Constructor injection
4. **Logging**: IGestLogLogger obligatorio
5. **Español**: UI y mensajes de usuario
6. **MVVM**: No lógica en code-behind
7. **Validación**: Antes de procesar
8. **Cancelación**: CancellationToken en operaciones largas

**Si viola SRP → Refactorizar inmediatamente**

---

*Actualizado: Junio 2025*
````
