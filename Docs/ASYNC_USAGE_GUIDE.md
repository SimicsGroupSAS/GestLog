# 🔧 Guía de Uso - Async/Await en GestLog

## 🚀 Nuevas Funcionalidades Async

### 1. **Procesamiento de Archivos Excel (Mejorado)**

```csharp
// Uso en ViewModels o Services
public async Task ProcessFilesAsync()
{
    var cancellationTokenSource = new CancellationTokenSource();
    var progress = new Progress<double>(p => Progress = p);
    
    try
    {
        // Procesamiento asíncrono
        var result = await _excelService.ProcesarArchivosExcelAsync(
            folderPath, 
            progress, 
            cancellationTokenSource.Token
        );
        
        // Generación asíncrona
        await _excelService.GenerarArchivoConsolidadoAsync(
            result, 
            outputPath, 
            cancellationTokenSource.Token
        );
    }
    catch (OperationCanceledException)
    {
        // Manejo de cancelación
    }
}
```

### 2. **Exportación de Excel (Nuevo Async)**

```csharp
// Exportación asíncrona con cancelación
var excelExportService = new ExcelExportService();
await excelExportService.ExportarConsolidadoAsync(
    dataTable, 
    outputPath, 
    cancellationToken
);
```

### 3. **Carga de Datos (FilteredDataView)**

```csharp
// Carga asíncrona sin bloquear UI
var data = await LoadConsolidatedExcelAsync(filePath);

// Exportación asíncrona de datos filtrados
await ExportFilteredDataToExcelAsync(filteredData);
```

## 🎛️ Funcionalidades de la UI

### **Controles de Estado**
- **Barra de Progreso**: Muestra el progreso actual de operaciones
- **Mensaje de Estado**: Indica qué está sucediendo en tiempo real
- **Botón de Cancelación**: Aparece durante operaciones para permitir cancelación

### **Binding XAML Mejorado**
```xml
<!-- Mensaje de estado (solo visible cuando hay mensaje) -->
<TextBlock Text="{Binding StatusMessage}" 
           Visibility="{Binding StatusMessage, Converter={x:Static Converters:StringToVisibilityConverter.Instance}}"/>

<!-- Botón de cancelación (solo visible durante procesamiento) -->
<Button Content="❌ Cancelar" 
        Command="{Binding CancelProcessingCommand}"
        Visibility="{Binding IsProcessing, Converter={x:Static Converters:BooleanToVisibilityConverter.Instance}}"/>
```

## 🔍 Mejores Prácticas Implementadas

### **1. Patrón Async/Await Correcto**
```csharp
public async Task ExampleAsync(CancellationToken cancellationToken = default)
{
    // ✅ Usar Task.Run para operaciones CPU-intensivas
    await Task.Run(() =>
    {
        // Verificar cancelación en puntos apropiados
        cancellationToken.ThrowIfCancellationRequested();
        
        // Trabajo pesado aquí
        ProcessHeavyWork();
        
        // Verificar nuevamente
        cancellationToken.ThrowIfCancellationRequested();
    }, cancellationToken);
}
```

### **2. Manejo de Cancelación**
```csharp
public async Task ProcessWithCancellationAsync()
{
    using var cts = new CancellationTokenSource();
    
    try
    {
        await SomeAsyncOperation(cts.Token);
    }
    catch (OperationCanceledException)
    {
        // Operación cancelada - esto es normal
        logger.Info("Operación cancelada por el usuario");
    }
    finally
    {
        // Cleanup automático con 'using'
    }
}
```

### **3. Reporte de Progreso**
```csharp
public async Task ProcessWithProgressAsync()
{
    var progress = new Progress<double>(percentage =>
    {
        // Actualizar UI en el hilo principal
        ProgressValue = percentage;
        StatusMessage = $"Procesando... {percentage:F1}%";
    });
    
    await LongRunningOperationAsync(progress);
}
```

## 📊 Medición de Rendimiento

### **Usar AsyncPerformanceTest**
```csharp
var performanceTest = new AsyncPerformanceTest();

// Probar procesamiento con cancelación
var result = await performanceTest.TestAsyncProcessingWithCancellationAsync(folderPath);
Console.WriteLine(result);

// Probar operaciones concurrentes
var concurrentResult = await performanceTest.TestConcurrentOperationsAsync();
Console.WriteLine(concurrentResult);
```

## 🛠️ Troubleshooting

### **Problemas Comunes y Soluciones**

#### **1. UI se Bloquea**
```csharp
// ❌ MAL - bloquea UI
var result = SomeAsyncMethod().Result;

// ✅ BIEN - no bloquea UI
var result = await SomeAsyncMethod();
```

#### **2. Cancelación no Funciona**
```csharp
// ❌ MAL - no verifica cancelación
for (int i = 0; i < 1000000; i++)
{
    DoWork();
}

// ✅ BIEN - verifica cancelación
for (int i = 0; i < 1000000; i++)
{
    cancellationToken.ThrowIfCancellationRequested();
    DoWork();
}
```

#### **3. Memory Leaks**
```csharp
// ❌ MAL - no dispose
var cts = new CancellationTokenSource();

// ✅ BIEN - dispose automático
using var cts = new CancellationTokenSource();
```

## 🔄 Comandos Async en MVVM

### **Implementación Correcta**
```csharp
[RelayCommand(CanExecute = nameof(CanExecuteOperation))]
public async Task ExecuteOperationAsync()
{
    IsProcessing = true;
    try
    {
        await DoAsyncWork();
    }
    finally
    {
        IsProcessing = false;
        ExecuteOperationCommand.NotifyCanExecuteChanged();
    }
}

private bool CanExecuteOperation() => !IsProcessing;
```

## 📝 Testing

### **Probar Funcionalidades Async**
```csharp
[Test]
public async Task TestAsyncOperation()
{
    // Arrange
    var service = new ExcelExportService();
    var testData = CreateTestData();
    var outputPath = "test.xlsx";
    
    // Act
    await service.ExportarConsolidadoAsync(testData, outputPath);
    
    // Assert
    Assert.That(File.Exists(outputPath), Is.True);
}
```

---

## 🎯 Resumen de Beneficios

✅ **UI Responsiva**: No más congelamiento durante operaciones largas  
✅ **Cancelación**: Usuario puede detener operaciones cuando quiera  
✅ **Feedback Visual**: Progreso en tiempo real  
✅ **Mejor Rendimiento**: Operaciones paralelas y optimizadas  
✅ **Código Mantenible**: Patrones async estándar implementados  
✅ **Escalabilidad**: Preparado para archivos grandes y operaciones complejas  

¡El sistema GestLog ahora utiliza las mejores prácticas de programación asíncrona en .NET 9.0!
