# Estandarización de Dependency Injection en GestLog

Este documento proporciona una guía completa para la implementación y estandarización del sistema de Dependency Injection (DI) en la aplicación GestLog.

## 1. Guía de Implementación Paso a Paso

### 1.1 Configuración del Contenedor de DI

#### Paso 1: Configurar el contenedor en App.xaml.cs
```csharp
// 1. Importar las librerías necesarias
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public partial class App : Application
{
    // 2. Crear el proveedor de servicios como propiedad estática
    public static ServiceProvider ServiceProvider { get; private set; }

    // 3. Inicializar los servicios en OnStartup
    protected override void OnStartup(StartupEventArgs e)
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        
        // 4. Construir el proveedor de servicios
        ServiceProvider = serviceCollection.BuildServiceProvider();
        
        // 5. Inicializar la ventana principal con DI
        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
    
    // 6. Método para registrar todos los servicios
    private void ConfigureServices(IServiceCollection services)
    {
        // 7. Registrar implementaciones concretas
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainViewModel>();
        
        // 8. Registrar servicios con interfaces
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IGestLogLogger, GestLogLogger>();
        services.AddTransient<IExcelExportService, ExcelExportService>();
        services.AddTransient<IExcelProcessingService, ExcelProcessingService>();
        services.AddTransient<IDataConsolidationService, DataConsolidationService>();
        services.AddTransient<IConsolidatedFilterService, ConsolidatedFilterService>();
        services.AddTransient<IErrorHandlingService, ErrorHandlingService>();
        
        // 9. Registrar servicios con configuración adicional
        services.AddLogging(configure => configure.AddFile("Logs/gestlog-{Date}.txt"));
    }
}

### 1.2 Implementación en Servicios

#### Paso 2: Creación de interfaces de servicios
```csharp
// 1. Definir interfaces claras para cada servicio
public interface IConsolidatedFilterService
{
    // 2. Definir métodos con contratos bien definidos
    FilteredDataViewModel ApplyFilters(DataTable data, FilterCriteria criteria);
    IEnumerable<DataRow> GetFilteredRows(DataTable data, FilterCriteria criteria);
    int GetFilteredRecordCount(DataTable data, FilterCriteria criteria);
}
```

#### Paso 3: Implementación de servicios con inyección
```csharp
// 1. Implementación del servicio con dependencias inyectadas
public class ConsolidatedFilterService : IConsolidatedFilterService
{
    // 2. Declarar dependencias como campos privados readonly
    private readonly IGestLogLogger _logger;
    private readonly IConfigurationService _configService;

    // 3. Recibir todas las dependencias en el constructor
    public ConsolidatedFilterService(
        IGestLogLogger logger,
        IConfigurationService configService)
    {
        // 4. Validar y asignar dependencias
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
    }

    // 5. Implementar métodos de la interfaz
    public FilteredDataViewModel ApplyFilters(DataTable data, FilterCriteria criteria)
    {
        // 6. Usar el logger para diagnóstico y traza
        _logger.Info($"Aplicando filtros a {data.Rows.Count} registros");
        
        try 
        {
            // 7. Usar configuración inyectada
            var filterConfig = _configService.Configuration.FilterSettings;
            var caseSensitive = filterConfig.IsCaseSensitive;
            
            // 8. Realizar operación principal
            var filteredRows = GetFilteredRows(data, criteria).ToList();
            
            _logger.Info($"Filtrado completado. Resultados: {filteredRows.Count} registros");
            
            // 9. Devolver modelo de vista con datos
            return new FilteredDataViewModel 
            { 
                FilteredData = filteredRows,
                FilterCriteria = criteria,
                TotalCount = data.Rows.Count,
                FilteredCount = filteredRows.Count,
                AppliedAt = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            // 10. Registro adecuado de errores
            _logger.Error(ex, "Error al aplicar filtros");
            throw;
        }
    }
}
```

### 1.3 Uso en ViewModels

#### Paso 4: Refactorización de ViewModels con DI
```csharp
public class MainViewModel : ObservableObject
{
    // 1. Declarar servicios como campos privados readonly
    private readonly IExcelProcessingService _excelService;
    private readonly IGestLogLogger _logger;
    private readonly IErrorHandlingService _errorHandler;
    
    // 2. Constructor con inyección de dependencias
    public MainViewModel(
        IExcelProcessingService excelService,
        IGestLogLogger logger,
        IErrorHandlingService errorHandler)
    {
        // 3. Asignar servicios inyectados (no crear manualmente)
        _excelService = excelService;
        _logger = logger;
        _errorHandler = errorHandler;
        
        // 4. Inicializar comandos y estado
        InitializeCommands();
    }
    
    private void InitializeCommands()
    {
        // 5. Los comandos usan los servicios inyectados
        ProcessExcelFilesCommand = new RelayCommand(ExecuteProcessExcelFiles, CanProcessExcelFiles);
    }
    
    private void ExecuteProcessExcelFiles()
    {
        try
        {
            // 6. Usar servicios inyectados
            _excelService.ProcessExcelFiles(SelectedFolder);
        }
        catch (Exception ex)
        {
            // 7. Usar el manejador de errores inyectado
            _errorHandler.HandleException(ex, "Procesamiento de archivos Excel");
        }
    }
}

### 1.4 Integración en la Aplicación

#### Paso 5: Acceso a servicios en vistas
```csharp
public partial class FilteredDataView : Window
{
    public FilteredDataView()
    {
        InitializeComponent();
        
        // 1. Obtener ViewModel del contenedor de DI
        var viewModel = App.ServiceProvider.GetRequiredService<FilteredDataViewModel>();
        
        // 2. Asignar como DataContext
        DataContext = viewModel;
        
        // 3. Inicializar con parámetros si es necesario
        viewModel.Initialize(SelectedDataTable);
    }
}
```

## 2. Ciclos de Vida de Servicios

| **Tipo** | **Duración** | **Uso Recomendado** | **En GestLog** |
|----------|--------------|----------------------|----------------|
| Singleton | Toda la aplicación | Servicios compartidos sin estado mutable | ConfigurationService, Logger |
| Transient | Nueva instancia cada vez | Servicios con estado o no compartibles | ExcelProcessingService, FilterService |
| Scoped | Duración de una operación | Servicios por contexto/operación | DataConsolidationService |

## 3. Pruebas de Servicios con DI

```csharp
[TestClass]
public class FilterServiceTests
{
    [TestMethod]
    public void ApplyFilters_WithValidCriteria_ReturnsFilteredData()
    {
        // 1. Configurar mocks para las dependencias
        var mockLogger = new Mock<IGestLogLogger>();
        var mockConfig = new Mock<IConfigurationService>();
        mockConfig.Setup(c => c.Configuration.FilterSettings.IsCaseSensitive)
                 .Returns(false);
        
        // 2. Crear servicio a probar con dependencias simuladas
        var filterService = new ConsolidatedFilterService(
            mockLogger.Object, 
            mockConfig.Object);
        
        // 3. Preparar datos de prueba
        var testData = CreateTestDataTable();
        var criteria = new FilterCriteria { 
            DateRange = new DateRange(DateTime.Now.AddDays(-10), DateTime.Now),
            SearchText = "test" 
        };
        
        // 4. Ejecutar método a probar
        var result = filterService.ApplyFilters(testData, criteria);
        
        // 5. Verificar comportamiento y resultados
        Assert.IsNotNull(result);
        Assert.IsTrue(result.FilteredCount <= testData.Rows.Count);
        mockLogger.Verify(l => l.Info(It.IsAny<string>()), Times.Exactly(2));
        mockConfig.Verify(c => c.Configuration.FilterSettings, Times.Once());
    }
}
```

## 4. Mejores Prácticas

### 4.1 Diseño de Servicios
- ✅ Define interfaces claras con métodos enfocados
- ✅ Evita dependencias circulares entre servicios
- ✅ Usa tipos inmutables cuando sea posible
- ✅ Implementa registro adecuado de errores y eventos

### 4.2 Inyección de Dependencias
- ✅ Valida dependencias en el constructor
- ✅ Usa readonly para campos de servicios
- ✅ Evita servicios estáticos o Singleton globales
- ✅ Inyecta solo lo necesario (no toda la aplicación)

### 4.3 Testing
- ✅ Crea pruebas unitarias para cada servicio
- ✅ Utiliza mocks para simular dependencias
- ✅ Verifica comportamiento de servicios en casos límite
- ✅ Prueba escenarios de error y recuperación

## 5. Beneficios del Sistema DI en GestLog

- ✅ **Mantenibilidad**: Código más modular y fácil de mantener
- ✅ **Testabilidad**: Pruebas unitarias más sencillas y efectivas
- ✅ **Flexibilidad**: Facilidad para reemplazar implementaciones
- ✅ **Gestión de Estado**: Control preciso del ciclo de vida de objetos
- ✅ **Diagnóstico**: Mejor capacidad de logging y traza de errores
- ✅ **Desarrollo en Equipo**: Interfaces claras para colaboración
