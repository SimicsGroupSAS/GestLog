# Instrucciones de GitHub Copilot para GestLog

## 🎯 Contexto del Proyecto

GestLog es una aplicación WPF modular y escalable para gestión empresarial integral que actúa como hub central integrando herramientas especializadas de procesamiento de datos y gestión de cartera. El sistema está diseñado con arquitectura modular, separación de responsabilidades y alta escalabilidad.

**Idioma del Proyecto**: Código en inglés, comentarios y UI en español (es-CO).

## 🏗️ Arquitectura y Patrones de Diseño

### Patrón MVVM Estricto
```csharp
// ✅ Estructura correcta de ViewModels
public class DocumentGenerationViewModel : BaseViewModel
{
    private readonly IPdfGeneratorService _pdfService;
    private readonly IGestLogLogger _logger;
    
    // Propiedades con notificación automática
    [ObservableProperty]
    private string _selectedFilePath;
    
    // Comandos asíncronos
    [RelayCommand]
    private async Task GenerateDocumentsAsync(CancellationToken cancellationToken)
    {
        // Implementación con manejo de errores y logging
    }
}

// ❌ Evitar lógica en Code-Behind
public partial class MyView : UserControl
{
    // Solo inicialización y binding, NO lógica de negocio
    public MyView()
    {
        InitializeComponent();
    }
}
```

### Inyección de Dependencias
```csharp
// ✅ Registro en App.xaml.cs
protected override void OnStartup(StartupEventArgs e)
{
    // Servicios core primero
    ServiceLocator.RegisterSingleton<IGestLogLogger, GestLogLogger>();
    ServiceLocator.RegisterSingleton<IConfigurationService, ConfigurationService>();
    
    // Servicios de módulos
    ServiceLocator.RegisterSingleton<IPdfGeneratorService, PdfGeneratorService>();
    ServiceLocator.RegisterSingleton<IDataProcessorService, DataProcessorService>();
    
    // ViewModels con resolución automática
    ServiceLocator.RegisterTransient<DocumentGenerationViewModel>();
}

// ✅ Constructor injection en servicios
public class PdfGeneratorService : IPdfGeneratorService
{
    private readonly IGestLogLogger _logger;
    private readonly IConfigurationService _config;
    
    public PdfGeneratorService(IGestLogLogger logger, IConfigurationService config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }
}
```

### Arquitectura Modular
```
Modules/[NombreModulo]/
├── ViewModels/          # Solo lógica de presentación
├── Services/            # Lógica de negocio y acceso a datos
├── Models/              # DTOs y entidades
└── Interfaces/          # Contratos de servicios
```

## 📝 Convenciones de Nomenclatura

### Espacios de Nombres Estándar
```csharp
// ViewModels
namespace GestLog.Modules.GestionCartera.ViewModels;

// Servicios  
namespace GestLog.Modules.GestionCartera.Services;

// Modelos
namespace GestLog.Modules.GestionCartera.Models;

// Servicios globales
namespace GestLog.Services.Configuration;
namespace GestLog.Services.Logging;
namespace GestLog.Services.Validation;
```

### Convenciones de Nombres
```csharp
// ✅ Servicios
public interface IPdfGeneratorService { }
public class PdfGeneratorService : IPdfGeneratorService { }

// ✅ ViewModels
public class DocumentGenerationViewModel : BaseViewModel { }

// ✅ Modelos
public class GeneratedPdfInfo { }
public class CompanyData { }

// ✅ Comandos asíncronos
[RelayCommand]
private async Task GenerateDocumentsAsync() { }

// ✅ Propiedades observables
[ObservableProperty]
private string _selectedFilePath;

// ✅ Eventos y campos privados
private readonly CancellationTokenSource _cancellationTokenSource;
public event EventHandler<ProgressEventArgs> ProgressChanged;
```

## 🔄 Programación Asíncrona (Async/Await)

### Patrones Obligatorios
```csharp
// ✅ SIEMPRE usar async/await para I/O
public async Task<List<GeneratedPdfInfo>> GenerateDocumentsAsync(
    string excelPath, 
    string outputPath, 
    CancellationToken cancellationToken = default)
{
    try
    {
        _logger.LogInformation("Iniciando generación de documentos desde {ExcelPath}", excelPath);
        
        // Validación de entrada
        if (string.IsNullOrWhiteSpace(excelPath))
            throw new ArgumentException("La ruta del archivo Excel es requerida", nameof(excelPath));
            
        // Operación I/O asíncrona
        var companies = await LoadCompaniesFromExcelAsync(excelPath, cancellationToken);
        
        var results = new List<GeneratedPdfInfo>();
        var progress = new Progress<int>(value => 
        {
            ProgressChanged?.Invoke(this, new ProgressEventArgs(value, companies.Count));
        });
        
        // Procesamiento con progreso y cancelación
        for (int i = 0; i < companies.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var result = await GenerateDocumentForClientAsync(companies[i], outputPath, cancellationToken);
            results.Add(result);
            
            ((IProgress<int>)progress).Report(i + 1);
        }
        
        _logger.LogInformation("Generación completada. {Count} documentos creados", results.Count);
        return results;
    }
    catch (OperationCanceledException)
    {
        _logger.LogWarning("Generación de documentos cancelada por el usuario");
        throw;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error durante la generación de documentos");
        throw;
    }
}

// ✅ ConfigureAwait(false) en bibliotecas
private async Task<byte[]> LoadFileAsync(string path)
{
    return await File.ReadAllBytesAsync(path).ConfigureAwait(false);
}

// ✅ CancellationToken en todos los métodos asíncronos
public async Task<ValidationResult> ValidateExcelStructureAsync(
    string filePath, 
    CancellationToken cancellationToken = default)
{
    // Implementación con soporte de cancelación
}
```

### Comandos Asíncronos en ViewModels
```csharp
public partial class DocumentGenerationViewModel : BaseViewModel
{
    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private async Task GenerateDocumentsAsync()
    {
        try
        {
            IsGenerating = true;
            GenerationProgress = 0;
            
            using var cts = new CancellationTokenSource();
            _currentCancellationToken = cts.Token;
            
            var result = await _pdfService.GenerateDocumentsAsync(
                SelectedFilePath, 
                OutputPath, 
                cts.Token);
                
            GeneratedDocuments = result;
            
            _logger.LogInformation("Generación completada exitosamente");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Operación cancelada por el usuario");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la generación");
            // Mostrar mensaje al usuario
            await ShowErrorMessageAsync("Error en la generación", ex.Message);
        }
        finally
        {
            IsGenerating = false;
            _currentCancellationToken = null;
        }
    }
    
    private bool CanGenerate() => !IsGenerating && !string.IsNullOrEmpty(SelectedFilePath);
}
```

## 📋 Sistema de Logging (IGestLogLogger)

### Uso Obligatorio del Logger
```csharp
// ✅ Inyección y uso correcto
public class PdfGeneratorService : IPdfGeneratorService
{
    private readonly IGestLogLogger _logger;
    
    public PdfGeneratorService(IGestLogLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<GeneratedPdfInfo> GenerateDocumentAsync(CompanyData company)
    {
        // Información general
        _logger.LogInformation("Generando PDF para empresa {CompanyName}", company.Name);
        
        // Debug para desarrollo
        _logger.LogDebug("Datos de empresa: {@Company}", company);
        
        // Warning para situaciones no ideales
        if (company.DebtAmount <= 0)
        {
            _logger.LogWarning("Empresa {CompanyName} tiene deuda <= 0: {Amount}", 
                company.Name, company.DebtAmount);
        }
        
        try
        {
            // Operación principal
            var result = await CreatePdfAsync(company);
            
            // Success con métricas
            _logger.LogInformation("PDF generado exitosamente para {CompanyName}. " +
                "Archivo: {FilePath}, Tamaño: {FileSize} bytes", 
                company.Name, result.FilePath, result.FileSize);
                
            return result;
        }
        catch (Exception ex)
        {
            // Error con contexto completo
            _logger.LogError(ex, "Error generando PDF para empresa {CompanyName}. " +
                "Datos: {@Company}", company.Name, company);
            throw;
        }
    }
}

// ✅ Logging estructurado con propiedades
_logger.LogInformation("Procesamiento Excel completado. " +
    "Archivo: {FilePath}, Filas: {RowCount}, Tiempo: {ElapsedMs}ms",
    filePath, rowCount, stopwatch.ElapsedMilliseconds);

// ✅ Logging de rendimiento
using var activity = _logger.BeginScope("GenerateDocuments");
var stopwatch = Stopwatch.StartNew();
try
{
    // Operación
    var result = await ProcessAsync();
    
    _logger.LogInformation("Operación completada en {ElapsedMs}ms", 
        stopwatch.ElapsedMilliseconds);
    return result;
}
catch (Exception ex)
{
    _logger.LogError(ex, "Operación falló después de {ElapsedMs}ms", 
        stopwatch.ElapsedMilliseconds);
    throw;
}
```

## ✅ Sistema de Validación

### Validación Declarativa
```csharp
// ✅ Atributos de validación en modelos
public class ConfigurationModel : INotifyDataErrorInfo
{
    [Required(ErrorMessage = "La ruta de salida es requerida")]
    [DirectoryExists(ErrorMessage = "El directorio no existe")]
    public string OutputPath { get; set; }
    
    [Required(ErrorMessage = "La plantilla es requerida")]
    [FileExists(ErrorMessage = "El archivo de plantilla no existe")]
    [FileExtension(".png", ErrorMessage = "La plantilla debe ser un archivo PNG")]
    public string TemplatePath { get; set; }
    
    [Range(1, 1000, ErrorMessage = "El número debe estar entre 1 y 1000")]
    public int MaxConcurrentOperations { get; set; }
    
    // Implementación de INotifyDataErrorInfo...
}

// ✅ Validación en servicios
public class ValidationService : IValidationService
{
    private readonly IGestLogLogger _logger;
    
    public async Task<ValidationResult> ValidateExcelStructureAsync(
        string filePath, 
        CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();
        
        try
        {
            _logger.LogInformation("Validando estructura de Excel: {FilePath}", filePath);
            
            // Validaciones específicas
            if (!File.Exists(filePath))
            {
                result.AddError("Archivo no encontrado", filePath);
                return result;
            }
            
            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(1);
            
            // Validar columnas requeridas
            var requiredColumns = new[] { "B", "C", "L", "M", "N", "O", "U" };
            foreach (var col in requiredColumns)
            {
                if (worksheet.Cell($"{col}1").IsEmpty())
                {
                    result.AddError($"Columna {col} requerida está vacía", col);
                }
            }
            
            _logger.LogInformation("Validación completada. Errores: {ErrorCount}", 
                result.Errors.Count);
                
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante validación de Excel");
            result.AddError("Error interno de validación", ex.Message);
            return result;
        }
    }
}
```

### Validación en ViewModels
```csharp
public partial class DocumentGenerationViewModel : BaseViewModel, INotifyDataErrorInfo
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Debe seleccionar un archivo Excel")]
    [FileExists(ErrorMessage = "El archivo seleccionado no existe")]
    private string _selectedFilePath;
    
    [ObservableProperty]
    [NotifyDataErrorInfo] 
    [Required(ErrorMessage = "Debe especificar una carpeta de salida")]
    [DirectoryExists(ErrorMessage = "La carpeta especificada no existe")]
    private string _outputPath;
    
    // Validación manual adicional
    private async Task ValidateInputsAsync()
    {
        ClearErrors();
        
        if (!string.IsNullOrEmpty(SelectedFilePath))
        {
            var validationResult = await _validationService
                .ValidateExcelStructureAsync(SelectedFilePath);
                
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    AddError(nameof(SelectedFilePath), error.Message);
                }
            }
        }
        
        OnPropertyChanged(nameof(HasErrors));
    }
}
```

## 🚨 Manejo de Errores Completo

### Jerarquía de Excepciones Personalizadas
```csharp
// ✅ Excepciones específicas del dominio
public class GestLogException : Exception
{
    public string ErrorCode { get; }
    
    public GestLogException(string message, string errorCode = null) : base(message)
    {
        ErrorCode = errorCode;
    }
    
    public GestLogException(string message, Exception innerException, string errorCode = null) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

public class ExcelValidationException : GestLogException
{
    public List<ValidationError> ValidationErrors { get; }
    
    public ExcelValidationException(string message, List<ValidationError> errors) 
        : base(message, "EXCEL_VALIDATION")
    {
        ValidationErrors = errors ?? new List<ValidationError>();
    }
}

public class PdfGenerationException : GestLogException
{
    public string CompanyName { get; }
    
    public PdfGenerationException(string message, string companyName) 
        : base(message, "PDF_GENERATION")
    {
        CompanyName = companyName;
    }
}
```

### Manejo de Errores en Servicios
```csharp
public class PdfGeneratorService : IPdfGeneratorService
{
    public async Task<GeneratedPdfInfo> GenerateDocumentAsync(
        CompanyData company, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Iniciando generación PDF para {CompanyName}", company.Name);
            
            // Validación de entrada
            if (company == null)
                throw new ArgumentNullException(nameof(company));
                
            if (string.IsNullOrWhiteSpace(company.Name))
                throw new ArgumentException("El nombre de la empresa es requerido", 
                    nameof(company));
            
            // Operación principal con manejo específico
            var pdfBytes = await CreatePdfContentAsync(company, cancellationToken);
            var filePath = Path.Combine(_outputPath, $"{company.Name}.pdf");
            
            await File.WriteAllBytesAsync(filePath, pdfBytes, cancellationToken);
            
            var fileInfo = new FileInfo(filePath);
            var result = new GeneratedPdfInfo
            {
                NombreEmpresa = company.Name,
                RutaArchivo = filePath,
                GeneratedDate = DateTime.Now,
                FileSize = fileInfo.Length,
                RecordCount = company.Records?.Count ?? 0
            };
            
            _logger.LogInformation("PDF generado exitosamente para {CompanyName}. " +
                "Archivo: {FilePath}, Tamaño: {FileSize}", 
                company.Name, filePath, fileInfo.Length);
                
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Generación de PDF cancelada para {CompanyName}", company.Name);
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            var errorMsg = $"Sin permisos para escribir el archivo PDF para {company.Name}";
            _logger.LogError(ex, errorMsg);
            throw new PdfGenerationException(errorMsg, company.Name);
        }
        catch (IOException ex)
        {
            var errorMsg = $"Error de I/O al generar PDF para {company.Name}";
            _logger.LogError(ex, errorMsg);
            throw new PdfGenerationException(errorMsg, company.Name);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error inesperado generando PDF para {company.Name}";
            _logger.LogError(ex, errorMsg);
            throw new PdfGenerationException(errorMsg, company.Name);
        }
    }
}
```

### Manejo de Errores en ViewModels
```csharp
public partial class DocumentGenerationViewModel : BaseViewModel
{
    [RelayCommand]
    private async Task GenerateDocumentsAsync()
    {
        try
        {
            IsGenerating = true;
            ErrorMessage = null;
            
            _logger.LogInformation("Iniciando generación de documentos desde UI");
            
            var result = await _pdfService.GenerateDocumentsAsync(
                SelectedFilePath, 
                OutputPath, 
                _cancellationTokenSource.Token);
                
            GeneratedDocuments = result;
            
            await ShowSuccessMessageAsync($"Se generaron {result.Count} documentos exitosamente");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Generación cancelada por el usuario");
            ErrorMessage = "Operación cancelada";
        }
        catch (ExcelValidationException ex)
        {
            _logger.LogWarning("Errores de validación en Excel: {ErrorCount}", 
                ex.ValidationErrors.Count);
            ErrorMessage = $"Excel inválido: {ex.Message}";
            await ShowValidationErrorsAsync(ex.ValidationErrors);
        }
        catch (PdfGenerationException ex)
        {
            _logger.LogError("Error generando PDF para {CompanyName}", ex.CompanyName);
            ErrorMessage = $"Error generando PDF: {ex.Message}";
            await ShowErrorMessageAsync("Error de Generación", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en generación de documentos");
            ErrorMessage = "Error inesperado. Ver logs para detalles.";
            await ShowErrorMessageAsync("Error", "Ha ocurrido un error inesperado");
        }
        finally
        {
            IsGenerating = false;
        }
    }
    
    private async Task ShowErrorMessageAsync(string title, string message)
    {
        // Mostrar mensaje en UI de forma no bloqueante
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        });
    }
}
```

## 📊 Indicadores de Progreso y Cancelación

```csharp
// ✅ Reporte de progreso estándar
public async Task<List<T>> ProcessItemsAsync<T>(
    IEnumerable<T> items,
    Func<T, CancellationToken, Task<T>> processor,
    IProgress<ProgressInfo> progress = null,
    CancellationToken cancellationToken = default)
{
    var itemList = items.ToList();
    var results = new List<T>();
    
    for (int i = 0; i < itemList.Count; i++)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        try
        {
            var result = await processor(itemList[i], cancellationToken);
            results.Add(result);
            
            // Reporte de progreso
            progress?.Report(new ProgressInfo
            {
                Current = i + 1,
                Total = itemList.Count,
                CurrentItem = itemList[i]?.ToString(),
                Status = "Procesando..."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando item {Index}: {Item}", i, itemList[i]);
            // Decidir si continuar o fallar
            throw;
        }
    }
    
    return results;
}

// ✅ Cancelación en ViewModels
private CancellationTokenSource _cancellationTokenSource;

[RelayCommand]
private void CancelOperation()
{
    _cancellationTokenSource?.Cancel();
    _logger.LogInformation("Cancelación solicitada por el usuario");
}
```

## 📋 Dependencias y Tecnologías

### Principales
- **.NET 9.0** con **WPF** - Framework principal
- **CommunityToolkit.Mvvm** - Patrón MVVM, ObservableProperty, RelayCommand
- **ClosedXML** - Lectura y escritura de archivos Excel
- **iText7** - Generación profesional de PDFs  
- **FuzzySharp** - Algoritmos de coincidencia difusa para normalización
- **Ookii.Dialogs.Wpf** - Diálogos nativos de Windows

### Patrones de Uso
```csharp
// ✅ ClosedXML
using var workbook = new XLWorkbook(filePath);
var worksheet = workbook.Worksheet(1);

// ✅ iText7
using var writer = new PdfWriter(outputPath);
using var pdf = new PdfDocument(writer);
using var document = new Document(pdf);

// ✅ CommunityToolkit.Mvvm
[ObservableProperty]
private string _property;

[RelayCommand]
private async Task CommandAsync() { }
```

## 🎯 Principios de Desarrollo

1. **Async/Await en Todo**: Todas las operaciones I/O deben ser asíncronas
2. **Logging Estructurado**: Usar propiedades estructuradas en logs
3. **Validación Primero**: Validar entradas antes de procesar
4. **Manejo de Excepciones**: Manejar excepciones específicas del dominio
5. **Soporte de Cancelación**: Soportar cancelación en operaciones largas
6. **Reporte de Progreso**: Reportar progreso en operaciones de UI
7. **Gestión de Recursos**: Usar `using` para recursos y dispose apropiado
8. **Separación de Responsabilidades**: ViewModels solo para UI, Services para lógica
9. **Inyección de Dependencias**: Resolver dependencias automáticamente
10. **Idioma Español**: Código en inglés, UI y mensajes en español

## 🚫 Anti-Patrones a Evitar

```csharp
// ❌ NO hacer
public void ProcessData() // Síncrono para I/O
{
    File.ReadAllText(path); // Bloquea UI
}

// ❌ NO hacer  
catch (Exception ex)
{
    // Log vacío o genérico
    Console.WriteLine("Error");
}

// ❌ NO hacer
public string FilePath { get; set; } // Sin validación

// ❌ NO hacer - lógica en code-behind
private void Button_Click(object sender, RoutedEventArgs e)
{
    var data = LoadData(); // Lógica de negocio en UI
}
```

## 📁 Estructura de Archivos Esperada

Seguir esta estructura para nuevos módulos:
```
Modules/[NombreModulo]/
├── ViewModels/
│   ├── [Funcionalidad]ViewModel.cs
│   └── Base/
├── Services/
│   ├── I[Servicio]Service.cs
│   ├── [Servicio]Service.cs
│   └── Models/
├── Models/
│   ├── [Entidad].cs
│   └── [Entidad]ValidationModel.cs
└── Interfaces/
    └── I[Servicio]Service.cs
```

Este archivo de instrucciones asegura que GitHub Copilot genere código consistente con la arquitectura, patrones y convenciones establecidas en GestLog.

## 💡 Mensajes y Textos de Usuario

**IMPORTANTE**: Todos los mensajes mostrados al usuario, nombres de controles, etiquetas, títulos de ventanas, mensajes de error y validación deben estar en español.

### Ejemplos de Mensajes Correctos:
```csharp
// ✅ Mensajes de error en español
ErrorMessage = "El archivo seleccionado no existe";
throw new ArgumentException("La ruta del archivo Excel es requerida", nameof(excelPath));

// ✅ Logs en español para contexto de usuario
_logger.LogInformation("Iniciando generación de documentos para {CompanyCount} empresas", companies.Count);

// ✅ Validación en español
[Required(ErrorMessage = "Debe seleccionar un archivo Excel")]
[FileExists(ErrorMessage = "El archivo seleccionado no existe")]

// ✅ Títulos y mensajes de UI
await ShowErrorMessageAsync("Error de Generación", "No se pudo generar el documento PDF");
MessageBox.Show("Operación completada exitosamente", "Éxito", MessageBoxButton.OK);
```

### Ejemplos de Logs Técnicos (pueden ser en inglés):
```csharp
// ✅ Logs técnicos en inglés
_logger.LogDebug("Processing Excel file: {FilePath}", filePath);
_logger.LogError(ex, "Error during PDF generation for company {CompanyName}", company.Name);
```
