# Sistema Global de Manejo de Errores - GestLog

## Descripción General

El sistema de manejo de errores de GestLog proporciona una forma unificada y centralizada de gestionar las excepciones y errores en toda la aplicación. Está diseñado para mejorar la experiencia del usuario final, facilitar la depuración y permitir un seguimiento eficiente de los problemas.

## Componentes Principales

### 1. ErrorHandlingService

Servicio central que implementa la interfaz `IErrorHandlingService` para gestionar errores de forma unificada en toda la aplicación.

**Características principales:**
- Manejo de operaciones sincrónicas y asincrónicas
- Registro de errores con información detallada
- Notificación a usuarios
- Propagación de errores mediante eventos
- Almacenamiento de historial de errores recientes

### 2. ErrorRecord

Modelo de datos que representa un registro de error y contiene toda la información relacionada.

**Propiedades:**
- Id (identificador único)
- Timestamp (fecha y hora del error)
- Context (contexto en el que ocurrió)
- Message (mensaje de error)
- ExceptionType (tipo de excepción)
- StackTrace (traza de la pila)

### 3. ErrorLogViewModel

ViewModel que gestiona la visualización y operaciones sobre el registro de errores.

**Funcionalidades:**
- Carga y actualización del registro de errores
- Manejo de selección de errores
- Comandos para copiar detalles de errores
- Actualización en tiempo real cuando ocurren nuevos errores

### 4. ErrorLogView

Ventana de interfaz de usuario para visualizar y gestionar el registro de errores.

**Características:**
- Vista detallada de los errores
- Filtrado y ordenación
- Copia de información detallada
- Vista de detalles para cada error seleccionado

## Cómo Usar el Sistema de Manejo de Errores

### 1. Manejo de operaciones propensas a errores

```csharp
// Inyectar el servicio
private readonly IErrorHandlingService _errorHandler;

// Operaciones sincrónicas
_errorHandler.HandleOperation(() => {
    // Código que podría generar excepciones
}, "Nombre de la operación");

// Operaciones asincrónicas
await _errorHandler.HandleOperationAsync(async () => {
    // Código asincrónico que podría generar excepciones
}, "Nombre de la operación asincrónica");

// Operaciones con valor de retorno
var result = _errorHandler.HandleOperation(() => {
    // Código que retorna un valor
    return someValue;
}, "Operación con retorno", defaultValue);
```

### 2. Manejo directo de excepciones

```csharp
try {
    // Código que podría generar excepciones
}
catch (Exception ex) {
    _errorHandler.HandleException(ex, "Contexto de la excepción", showToUser: true);
}
```

### 3. Mostrar el registro de errores

```csharp
private void ShowErrorLog()
{
    var errorLogVM = new ErrorLogViewModel();
    var errorLogView = new ErrorLogView(errorLogVM);
    errorLogView.ShowErrorLog(this); // 'this' es la ventana propietaria
}
```

### 4. Suscribirse a eventos de error

```csharp
_errorHandler.ErrorOccurred += (sender, e) => {
    // Reaccionar cuando ocurre un nuevo error
    Console.WriteLine($"Nuevo error: {e.Error.Message}");
};
```

## Mejores Prácticas

1. **Nombres de contexto descriptivos**: Use nombres de contexto claros y específicos para facilitar la depuración.

2. **Mostrar errores al usuario selectivamente**: Configure `showToUser` a `true` solo para errores relevantes para el usuario final.

3. **Proporcione valores predeterminados**: Para operaciones que retornan valores, proporcione siempre un valor predeterminado adecuado.

4. **Verificación de errores**: Revise periódicamente el registro de errores para detectar problemas sistemáticos.

5. **Actualice el registro de errores**: Implemente la actualización periódica del registro de errores en áreas críticas de la aplicación.

## Patrones de Error Comunes y Soluciones

| Tipo de Error | Posibles Causas | Soluciones Recomendadas |
|---------------|-----------------|-------------------------|
| IOException | Problemas de acceso a archivos | Verificar permisos y disponibilidad de archivos |
| NetworkException | Problemas de conectividad | Comprobar la conexión a internet y el estado del servidor |
| InvalidOperationException | Secuencia de operaciones incorrecta | Revisar la lógica de negocio y el flujo de la aplicación |
| NullReferenceException | Referencias a objetos no inicializados | Implementar comprobaciones de nulidad y patrones de inicialización seguros |

## Conclusión

El sistema de manejo de errores de GestLog proporciona una solución robusta y centralizada para la gestión de errores, mejorando la experiencia del usuario y facilitando el mantenimiento y depuración de la aplicación.
