# Sistema de Manejo de Errores - Implementación y Pruebas

## 1. Guía de Implementación

### 1.1 Arquitectura del Sistema de Errores

El sistema de manejo de errores de GestLog se implementa en tres niveles:

1. **Captura de Excepciones**: Centralización de toda la lógica de manejo de excepciones
2. **Registro de Errores**: Almacenamiento persistente de información detallada
3. **Interfaz de Usuario**: Presentación y navegación de errores registrados

### 1.2 Implementación Paso a Paso

#### Paso 1: Crear el Servicio de Manejo de Errores
```csharp
// 1. Definir una interfaz clara
public interface IErrorHandlingService
{
    // 2. Método principal para manejar excepciones
    void HandleException(Exception ex, string context);
    
    // 3. Métodos adicionales para consulta y reportes
    IEnumerable<ErrorRecord> GetRecentErrors(int count = 100);
    ErrorRecord GetErrorDetails(Guid errorId);
    void ClearErrors(DateTime olderThan);
}

// 4. Implementación concreta del servicio
public class ErrorHandlingService : IErrorHandlingService
{
    // 5. Dependencias necesarias
    private readonly IGestLogLogger _logger;
    private readonly IErrorRepository _repository;
    
    // 6. Inyección de dependencias
    public ErrorHandlingService(
        IGestLogLogger logger,
        IErrorRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }
    
    // 7. Implementación del método principal
    public void HandleException(Exception ex, string context)
    {
        try
        {
            // 8. Crear registro de error con toda la información relevante
            var errorRecord = new ErrorRecord
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.Now,
                ExceptionType = ex.GetType().FullName,
                Message = ex.Message,
                StackTrace = ex.StackTrace,
                Context = context,
                InnerExceptionMessage = ex.InnerException?.Message,
                AdditionalData = CollectAdditionalData(ex)
            };
            
            // 9. Registrar error en múltiples destinos
            _logger.Error(ex, $"[{context}] {ex.Message}");
            _repository.SaveError(errorRecord);
            
            // 10. Notificar sobre el error si es crítico
            if (IsCriticalError(ex))
            {
                NotifyCriticalError(errorRecord);
            }
        }
        catch (Exception logEx)
        {
            // 11. Tratamiento de errores durante el manejo de errores
            _logger.Fatal(logEx, "Error al registrar excepción");
        }
    }
    
    // Implementación de métodos adicionales...
}
```

#### Paso 2: Integrar Manejo de Errores en la UI
```csharp
// 1. Implementar ViewModel para visualización de errores
public class ErrorLogViewModel : ObservableObject
{
    // 2. Dependencias y colecciones
    private readonly IErrorHandlingService _errorService;
    public ObservableCollection<ErrorRecordViewModel> Errors { get; }
    
    // 3. Constructor con inyección
    public ErrorLogViewModel(IErrorHandlingService errorService)
    {
        _errorService = errorService;
        Errors = new ObservableCollection<ErrorRecordViewModel>();
        
        // 4. Comandos
        ViewErrorDetailsCommand = new RelayCommand<ErrorRecordViewModel>(ExecuteViewErrorDetails);
        CopyErrorDetailsCommand = new RelayCommand<ErrorRecordViewModel>(ExecuteCopyErrorDetails);
    }
    
    // 5. Método de inicialización
    public void LoadRecentErrors()
    {
        var recentErrors = _errorService.GetRecentErrors(100);
        
        Errors.Clear();
        foreach (var error in recentErrors)
        {
            Errors.Add(new ErrorRecordViewModel(error));
        }
    }
    
    // Implementación de comandos y métodos adicionales...
}
```

#### Paso 3: Implementar Vista de Errores
```xml
<Window x:Class="GestLog.Views.ErrorLogView"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Registro de Errores" Height="600" Width="800">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="300"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>
        
        <!-- Lista de errores en el panel izquierdo -->
        <ListView Grid.Column="0" 
                  ItemsSource="{Binding Errors}"
                  SelectedItem="{Binding SelectedError}">
            <ListView.ItemTemplate>
                <DataTemplate>
                    <StackPanel>
                        <TextBlock Text="{Binding Timestamp, StringFormat='dd/MM/yyyy HH:mm'}" 
                                   FontWeight="Bold"/>
                        <TextBlock Text="{Binding ExceptionType}" 
                                   Foreground="#D32F2F"/>
                        <TextBlock Text="{Binding Message}" 
                                   TextWrapping="Wrap"/>
                    </StackPanel>
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
        
        <!-- Detalles del error en el panel derecho -->
        <Grid Grid.Column="1" DataContext="{Binding SelectedError}">
            <!-- Implementación de detalles con formato adecuado -->
        </Grid>
    </Grid>
</Window>
```

## 2. Instrucciones de Prueba

### 2.1 Pruebas Automatizadas

```csharp
[TestClass]
public class ErrorHandlingSystemTests
{
    [TestMethod]
    public void ErrorHandling_CapturesAndLogsException()
    {
        // 1. Configurar
        var mockLogger = new Mock<IGestLogLogger>();
        var mockRepository = new Mock<IErrorRepository>();
        
        var errorHandler = new ErrorHandlingService(
            mockLogger.Object,
            mockRepository.Object);
            
        var testException = new InvalidOperationException("Prueba de error");
        var testContext = "Test de manejo de errores";
        
        // 2. Ejecutar
        errorHandler.HandleException(testException, testContext);
        
        // 3. Verificar
        mockLogger.Verify(l => l.Error(
            It.Is<Exception>(e => e.Message == "Prueba de error"), 
            It.Is<string>(s => s.Contains(testContext))), 
            Times.Once);
            
        mockRepository.Verify(r => r.SaveError(
            It.Is<ErrorRecord>(rec => 
                rec.ExceptionType == typeof(InvalidOperationException).FullName &&
                rec.Message == "Prueba de error" &&
                rec.Context == testContext)), 
            Times.Once);
    }
    
    [TestMethod]
    public void ErrorHandler_HandlesNestedExceptions()
    {
        // Configuración similar
        var innerException = new FileNotFoundException("Archivo no encontrado");
        var outerException = new ApplicationException("Error de aplicación", innerException);
        
        // Ejecutar y verificar que se captura información del innerException
    }
    
    // Más pruebas...
}
```

### 2.2 Pruebas Manuales de UI

#### Paso 1: Verificar Navegación a Errores
1. Iniciar la aplicación GestLog
2. Hacer clic en el botón "Herramientas" en el menú principal
3. Verificar que existe una sección "Registro de Errores" en el panel de herramientas
4. Comprobar que el botón "Ver Errores" está presente y es consistente con el estilo de la aplicación

#### Paso 2: Generar Errores de Prueba
1. En el panel de herramientas, buscar la sección "Pruebas del Sistema"
2. Hacer clic en el botón "Ejecutar Prueba de Errores"
3. Verificar que aparece un mensaje indicando "Prueba completada con 5 errores registrados"
4. Los errores deben ser generados en distintas partes de la aplicación:
   - Error de acceso a archivo inexistente
   - Error de conversión de tipo
   - Error de validación de datos
   - Error de conexión simulada
   - Error de operación no soportada

#### Paso 3: Verificar Registro de Errores
1. Hacer clic en "Ver Errores" en la sección "Registro de Errores"
2. Verificar que se abre una ventana modal con título "Registro de Errores"
3. Comprobar que la ventana contiene:
   - Lista de errores en el panel izquierdo
   - Panel de detalles a la derecha
   - Botones de acción en la parte inferior

4. Verificar en la lista de errores:
   - Cada error muestra fecha/hora, tipo y mensaje resumido
   - Los errores están ordenados por fecha (más reciente primero)
   - Los errores de prueba aparecen en la lista
   - Se pueden seleccionar elementos de la lista

#### Paso 4: Examinar Detalles de Error
1. Seleccionar un error de la lista
2. Verificar que el panel derecho muestra los siguientes detalles:
   - ID único (formato GUID)
   - Fecha y hora exacta del error
   - Tipo de excepción (nombre completo de clase)
   - Mensaje de error completo
   - Contexto donde ocurrió el error
   - Stack trace completo
   - Información de excepción interna (si existe)
   - Datos adicionales capturados (como usuario, módulo, etc.)

#### Paso 5: Probar Funcionalidades Adicionales
1. **Copiar Detalles**:
   - Seleccionar un error
   - Hacer clic en botón "Copiar Detalles"
   - Verificar mensaje de confirmación
   - Pegar en editor de texto y verificar contenido completo

2. **Filtros**:
   - Usar el cuadro de búsqueda para filtrar por texto
   - Verificar que la lista se actualiza correctamente
   - Probar filtros por tipo de error y período de tiempo

3. **Exportación**:
   - Hacer clic en "Exportar Registro"
   - Especificar ubicación y formato (CSV/JSON)
   - Verificar que el archivo generado contiene todos los datos

3. **Actualización del Registro**:
   - Hacer clic en "Actualizar" en la ventana de registro de errores
   - Verificar que el indicador de carga aparece brevemente
   - Comprobar que la lista se actualiza y muestra un mensaje con el número de errores cargados

### 4. Pruebas Adicionales

1. **Limpieza de Selección**:
   - Seleccionar un error
   - Hacer clic en "Limpiar Selección"
   - Verificar que el panel de detalles se vacía

2. **Generación de Nuevos Errores**:
   - Ejecutar nuevamente las pruebas desde la pantalla principal
   - Volver a abrir el registro de errores
   - Verificar que los nuevos errores aparecen al principio de la lista

3. **Cierre de la Ventana**:
   - Cerrar la ventana de registro de errores
   - Verificar que la aplicación principal sigue funcionando correctamente

## Verificación de Funcionalidad Técnica

Para desarrolladores, las siguientes validaciones técnicas son importantes:

1. **Revisar los Archivos de Registro**:
   - Explorar la carpeta `Logs` de la aplicación
   - Verificar que los errores generados están siendo registrados correctamente en los archivos de log

2. **Revisión del Código de Ejemplo**:
   - Consultar `Examples/ErrorHandlingExample.cs` para ver ejemplos de uso del sistema
   - Verificar que el código de ejemplo está actualizado y funciona correctamente

3. **Pruebas de Integración**:
   - Verificar que el sistema de manejo de errores se integra correctamente con el servicio de registro de logs
   - Comprobar que los errores generados en diferentes partes de la aplicación se manejan de forma coherente

## Resultados Esperados

Al completar todas las pruebas, se espera:

1. Los errores se manejan de forma consistente en toda la aplicación
2. La interfaz de usuario proporciona fácil acceso al registro de errores desde la sección de herramientas
3. Los usuarios pueden revisar, filtrar y obtener detalles de los errores ocurridos
4. El sistema mantiene un registro histórico de los errores para su análisis

Si se observa alguna discrepancia respecto a estos resultados esperados, por favor documentar el problema detalladamente y reportarlo al equipo de desarrollo.
