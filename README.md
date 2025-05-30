# GestLog - Aplicación Modular Escalable

## Descripción

GestLog es una aplicación modular escalable que actúa como programa principal con un sistema de menús que puede integrar múltiples proyectos de software existentes. Todos los proyectos funcionan como un programa unificado con vistas, funciones y navegación compartidas a través de un menú principal.

## Estructura del Proyecto

```
GestLog/
├── App.xaml/cs                    # Definición principal de la aplicación
├── MainWindow.xaml/cs             # Ventana principal con navegación
├── Views/                         # Vistas de integración
│   ├── HerramientasView.xaml/cs   # Vista del menú de herramientas
│   └── DaaterProccesorView.xaml/cs # Vista de integración para DaaterProccesor
├── Assets/                        # Recursos compartidos (iconos, imágenes)
├── Data/                          # Archivos de datos compartidos
├── Modules/                       # Carpeta para módulos organizados por funcionalidad
│   └── DaaterProccesor/           # Módulo de procesamiento de datos
│       ├── ViewModels/            # ViewModels con namespace GestLog.Modules.DaaterProccesor.ViewModels
│       ├── Services/              # Servicios con namespace GestLog.Modules.DaaterProccesor.Services
│       └── App_Original.xaml.bak  # Respaldo del App.xaml original
└── bin/Debug/net9.0-windows/      # Salida de compilación
```

## Funcionalidades Implementadas

### 1. Navegación Principal
- **MainWindow**: Ventana principal con botón "Herramientas" y ContentControl para carga dinámica de contenido
- **HerramientasView**: Vista de menú de herramientas que lista todos los módulos disponibles
- **Navegación Dinámica**: Sistema de SetContent() para cargar UserControls dinámicamente

### 2. Integración de Módulos
- **DaaterProccesor**: Primer módulo integrado exitosamente
  - Procesamiento de archivos Excel
  - Vista de consolidación filtrada
  - Servicios de datos y filtrado
  - Algoritmos de coincidencia fuzzy

### 3. Gestión de Recursos
- **Assets**: Logo, iconos compartidos
- **Data**: Archivos Excel de configuración (países ISO, partidas arancelarias, datos de exportación)
- **Packages**: ClosedXML, CommunityToolkit.Mvvm, Ookii.Dialogs.Wpf, FuzzySharp

## Cómo Agregar Nuevos Módulos

### Paso 1: Preparar el Módulo
1. Crear carpeta en `Modules/[NombreModulo]/`
2. Copiar archivos del proyecto original a subcarpetas organizadas:
   - `Views/` → `ViewModels/`
   - `ViewModels/` → `ViewModels/`
   - `Services/` → `Services/`

### Paso 2: Organizar Namespaces
1. Actualizar todos los namespaces usando estructura jerárquica:
   - `[ProyectoOriginal].Views` → `GestLog.Views.Tools.[NombreModulo]`
   - `[ProyectoOriginal].ViewModels` → `GestLog.Modules.[NombreModulo].ViewModels`
   - `[ProyectoOriginal].Services` → `GestLog.Modules.[NombreModulo].Services`

### Paso 3: Configurar Proyecto
1. Los archivos se incluyen automáticamente con la configuración estándar de WPF SDK
2. El proyecto está configurado para auto-descubrimiento de archivos

### Paso 4: Crear Vista de Integración
1. Crear vistas organizadas jerárquicamente en `Views/Tools/[NombreModulo]/`
2. Implementar como UserControl con referencias a los módulos organizados
3. Agregar botón en `HerramientasView.xaml` para acceder al módulo

### Paso 5: Agregar Recursos
1. Copiar assets necesarios a `Assets/`
2. Copiar datos necesarios a `Data/`
3. Instalar paquetes NuGet requeridos

## Compilación y Ejecución

```powershell
# Compilar
dotnet build

# Ejecutar
dotnet run
```

## Estado Actual

✅ **Completado:**
- Estructura modular escalable
- Integración exitosa de DaaterProccesor
- Sistema de navegación dinámico
- Migración completa de namespaces
- Gestión de recursos y datos
- Compilación y ejecución exitosa

✅ **Probado:**
- Navegación desde menú principal a herramientas
- Carga dinámica de módulos
- Integración completa de funcionalidades de DaaterProccesor

## Tecnologías Utilizadas

- **.NET 9.0** con WPF
- **CommunityToolkit.Mvvm** para patrón MVVM
- **ClosedXML** para procesamiento de Excel
- **Ookii.Dialogs.Wpf** para diálogos nativos
- **FuzzySharp** para algoritmos de coincidencia

## Próximos Pasos

1. Documentar patrones específicos para tipos comunes de módulos
2. Crear templates para facilitar la integración
3. Implementar sistema de configuración compartida
4. Agregar logging centralizado
5. Implementar temas/estilos compartidos
