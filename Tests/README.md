# Tests

Esta carpeta está reservada para futuros tests unitarios del proyecto GestLog.

## Estructura Planificada

Cuando se implementen los tests unitarios, se recomienda la siguiente estructura:

```
Tests/
├── UnitTests/           # Tests unitarios rápidos y aislados
│   ├── Services/        # Tests para servicios
│   ├── ViewModels/      # Tests para ViewModels
│   ├── Models/          # Tests para modelos
│   └── Utilities/       # Tests para utilidades
├── IntegrationTests/    # Tests de integración
├── TestUtilities/       # Helpers y utilidades para tests
└── TestData/           # Datos de prueba
```

## Framework Recomendado

Se recomienda usar **xUnit** como framework de testing por ser el estándar moderno para .NET:

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
<PackageReference Include="xunit" Version="2.4.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.4.5" />
<PackageReference Include="Moq" Version="4.20.69" /> <!-- Para mocking -->
<PackageReference Include="FluentAssertions" Version="6.12.0" /> <!-- Para assertions más legibles -->
```

## Convenciones

- Los archivos de test deben terminar con `Test.cs` o `Tests.cs`
- Una clase de test por cada clase que se está probando
- Métodos de test con nombres descriptivos que expliquen qué se está probando
- Usar patrón Arrange-Act-Assert (AAA)

## Ejemplo de Test Unitario

```csharp
using Xunit;
using FluentAssertions;
using GestLog.Services;

namespace GestLog.Tests.UnitTests.Services
{
    public class ConfigurationServiceTests
    {
        [Fact]
        public void LoadConfiguration_WhenFileExists_ShouldReturnValidConfiguration()
        {
            // Arrange
            var service = new ConfigurationService();
            
            // Act
            var result = service.LoadConfiguration("valid-config.json");
            
            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeTrue();
        }
    }
}
```

Esta estructura garantiza tests mantenibles, rápidos y confiables.
