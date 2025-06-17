# =============================================================================
# SCRIPT DE CONFIGURACIÓN DE VARIABLES DE ENTORNO PARA GESTLOG
# =============================================================================
# Este script configura las variables de entorno necesarias para la 
# configuración segura de la base de datos de GestLog
#
# NOTA: A partir de la versión con First Run Setup automático, este script
# es OPCIONAL. GestLog configurará automáticamente las variables en la primera
# ejecución usando el sistema híbrido (archivo de configuración + valores hardcodeados).
#
# Usar este script solo si necesita:
# - Configuración manual personalizada
# - Cambiar credenciales existentes
# - Configurar entorno de desarrollo/testing
#
# Modo de uso:
# 1. Ejecutar: Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
# 2. Ejecutar: .\setup-environment-variables.ps1
# 3. Seguir las instrucciones en pantalla
# =============================================================================

param(
    [Parameter(HelpMessage="Usar configuración automática con valores predeterminados")]
    [switch]$Auto,
      [Parameter(HelpMessage="Servidor de base de datos")]
    [string]$Server = "SIMICSGROUPWKS1\SIMICSBD",
    
    [Parameter(HelpMessage="Nombre de la base de datos")]
    [string]$Database = "BD_ Pruebas",
    
    [Parameter(HelpMessage="Usuario de la base de datos (déjelo vacío para Windows Auth)")]
    [string]$Username = "sa",
    
    [Parameter(HelpMessage="Contraseña de la base de datos")]
    [string]$Password = "S1m1cS!DB_2025",
    
    [Parameter(HelpMessage="Usar autenticación integrada de Windows")]
    [switch]$UseWindowsAuth
)

Write-Host "==============================================================================" -ForegroundColor Cyan
Write-Host "      CONFIGURACIÓN DE VARIABLES DE ENTORNO PARA GESTLOG" -ForegroundColor Cyan
Write-Host "==============================================================================" -ForegroundColor Cyan
Write-Host ""

if ($Auto) {
    Write-Host "🔧 Configuración automática activada" -ForegroundColor Green
    Write-Host "   - Servidor: $Server" -ForegroundColor Gray
    Write-Host "   - Base de datos: $Database" -ForegroundColor Gray
    
    if ($UseWindowsAuth) {
        Write-Host "   - Autenticación: Windows (Integrada)" -ForegroundColor Gray
    } else {
        Write-Host "   - Usuario: $Username" -ForegroundColor Gray
        Write-Host "   - Contraseña: [OCULTA]" -ForegroundColor Gray
    }
    
    Write-Host ""
    $confirm = Read-Host "¿Continuar con esta configuración? (S/N)"
    if ($confirm -ne 'S' -and $confirm -ne 's') {
        Write-Host "❌ Configuración cancelada" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "🛠️  Configuración manual" -ForegroundColor Yellow
    Write-Host ""
    
    # Solicitar configuración al usuario
    $Server = Read-Host "Servidor de base de datos [$Server]"
    if ([string]::IsNullOrWhiteSpace($Server)) { $Server = "localhost" }
    
    $Database = Read-Host "Nombre de la base de datos [$Database]"
    if ([string]::IsNullOrWhiteSpace($Database)) { $Database = "GestLog" }
    
    $authChoice = Read-Host "Tipo de autenticación (1=SQL Server, 2=Windows) [1]"
    if ($authChoice -eq "2") {
        $UseWindowsAuth = $true
        Write-Host "✅ Usando autenticación de Windows" -ForegroundColor Green
    } else {
        $Username = Read-Host "Usuario de la base de datos [$Username]"
        if ([string]::IsNullOrWhiteSpace($Username)) { $Username = "gestlog_user" }
        
        $Password = Read-Host "Contraseña de la base de datos" -AsSecureString
        $Password = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password))
        
        if ([string]::IsNullOrWhiteSpace($Password)) {
            Write-Host "❌ La contraseña no puede estar vacía para autenticación SQL Server" -ForegroundColor Red
            exit 1
        }
    }
}

Write-Host ""
Write-Host "⚙️  Configurando variables de entorno..." -ForegroundColor Yellow

try {
    # Variables básicas
    [Environment]::SetEnvironmentVariable("GESTLOG_DB_SERVER", $Server, "User")
    [Environment]::SetEnvironmentVariable("GESTLOG_DB_NAME", $Database, "User")
    [Environment]::SetEnvironmentVariable("GESTLOG_DB_USE_INTEGRATED_SECURITY", $UseWindowsAuth.ToString().ToLower(), "User")
    [Environment]::SetEnvironmentVariable("GESTLOG_DB_CONNECTION_TIMEOUT", "30", "User")
    [Environment]::SetEnvironmentVariable("GESTLOG_DB_COMMAND_TIMEOUT", "300", "User")
    [Environment]::SetEnvironmentVariable("GESTLOG_DB_TRUST_CERTIFICATE", "true", "User")
    [Environment]::SetEnvironmentVariable("GESTLOG_DB_ENABLE_SSL", "true", "User")
    
    if (-not $UseWindowsAuth) {
        # Credenciales de SQL Server
        [Environment]::SetEnvironmentVariable("GESTLOG_DB_USER", $Username, "User")
        [Environment]::SetEnvironmentVariable("GESTLOG_DB_PASSWORD", $Password, "User")
        Write-Host "✅ Variables configuradas para autenticación SQL Server" -ForegroundColor Green
    } else {
        # Limpiar credenciales para Windows Auth
        [Environment]::SetEnvironmentVariable("GESTLOG_DB_USER", $null, "User")
        [Environment]::SetEnvironmentVariable("GESTLOG_DB_PASSWORD", $null, "User")
        Write-Host "✅ Variables configuradas para autenticación de Windows" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "🔍 Probando conexión a la base de datos..." -ForegroundColor Yellow
    
    # Construir cadena de conexión para prueba
    if ($UseWindowsAuth) {
        $connectionString = "Server=$Server;Database=$Database;Integrated Security=true;Connection Timeout=5;TrustServerCertificate=true;"
    } else {
        $connectionString = "Server=$Server;Database=$Database;User Id=$Username;Password=$Password;Connection Timeout=5;TrustServerCertificate=true;"
    }
    
    # Probar conexión
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $connection.Close()
    
    Write-Host "✅ ¡Conexión exitosa!" -ForegroundColor Green
    Write-Host ""
    Write-Host "==============================================================================" -ForegroundColor Cyan
    Write-Host "                    CONFIGURACIÓN COMPLETADA EXITOSAMENTE" -ForegroundColor Green
    Write-Host "==============================================================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Las siguientes variables de entorno han sido configuradas:" -ForegroundColor White
    Write-Host "  • GESTLOG_DB_SERVER = $Server" -ForegroundColor Gray
    Write-Host "  • GESTLOG_DB_NAME = $Database" -ForegroundColor Gray
    Write-Host "  • GESTLOG_DB_USE_INTEGRATED_SECURITY = $($UseWindowsAuth.ToString().ToLower())" -ForegroundColor Gray
    
    if (-not $UseWindowsAuth) {
        Write-Host "  • GESTLOG_DB_USER = $Username" -ForegroundColor Gray
        Write-Host "  • GESTLOG_DB_PASSWORD = [CONFIGURADA]" -ForegroundColor Gray
    }
    
    Write-Host "  • GESTLOG_DB_CONNECTION_TIMEOUT = 30" -ForegroundColor Gray
    Write-Host "  • GESTLOG_DB_COMMAND_TIMEOUT = 300" -ForegroundColor Gray
    Write-Host "  • GESTLOG_DB_TRUST_CERTIFICATE = true" -ForegroundColor Gray
    Write-Host "  • GESTLOG_DB_ENABLE_SSL = true" -ForegroundColor Gray
    Write-Host ""
    Write-Host "🚀 GestLog está listo para usar con configuración segura de base de datos." -ForegroundColor Green
    Write-Host ""
    
} catch {
    Write-Host ""
    Write-Host "❌ Error al configurar las variables de entorno:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Posibles soluciones:" -ForegroundColor Yellow
    Write-Host "  1. Verificar que SQL Server esté ejecutándose" -ForegroundColor Gray
    Write-Host "  2. Verificar las credenciales de acceso" -ForegroundColor Gray
    Write-Host "  3. Verificar la conectividad de red al servidor" -ForegroundColor Gray
    Write-Host "  4. Verificar que la base de datos '$Database' exista" -ForegroundColor Gray
    exit 1
}
