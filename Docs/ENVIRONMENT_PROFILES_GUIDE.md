# 🚀 Guía: Ejecutar GestLog en Development vs Production

## 📋 Resumen

GestLog detecta automáticamente el ambiente y carga la configuración correcta:

| Variable | Development | Production |
|----------|-------------|-----------|
| **GESTLOG_ENVIRONMENT** | `Development` | `Production` |
| **Base de Datos** | `GestLogDB_DEV` *(automático)* | `GestLogDB` *(automático)* |
| **Usuario de Prueba** | `admin` / `admin123` | Credenciales reales |
| **Actualizaciones** | ❌ Deshabilitadas | ✅ Habilitadas |

---

## 🎯 Cómo Ejecutar (Lo Fácil ✨)

### **Opción 1: Desde PowerShell/Terminal** ⭐ RECOMENDADO

#### **Development** (BD de Pruebas)
```powershell
cd "e:\Softwares\GestLog"
dotnet run --launch-profile Development
```

✅ Automáticamente conecta a: `GestLogDB_DEV`

#### **Production** (BD Principal)
```powershell
cd "e:\Softwares\GestLog"
dotnet run --launch-profile Production
```

✅ Automáticamente conecta a: `GestLogDB`

---

### **Opción 2: Desde Visual Studio**

**En la barra de herramientas:**

1. Busca el dropdown que dice el nombre del perfil (esquina superior izquierda)
2. Selecciona **"Development"** o **"Production"**
3. Presiona F5

---

### **Opción 3: Establecer Variable de Entorno Manualmente** (Opcional)

Si prefieres ejecutar siempre con `dotnet run` sin especificar perfil:

```powershell
# Hacer permanente
[Environment]::SetEnvironmentVariable("GESTLOG_ENVIRONMENT", "Development", "User")

# O solo para la sesión actual
$env:GESTLOG_ENVIRONMENT = "Development"
```

Luego ejecuta: `dotnet run`

**El resto se carga automáticamente desde:**
- `config/database-development.json` (si Development)
- `config/database-production.json` (si Production)

---

## 📊 ¿Cómo Funciona Automáticamente?

### **El Flujo:**

```
1. Estableces: GESTLOG_ENVIRONMENT = "Development"
                          ↓
2. GestLog detecta el ambiente
                          ↓
3. Lee automáticamente: config/database-development.json
                          ↓
4. Conecta a: GestLogDB_DEV con credenciales de ese archivo
```

### **Archivos Automáticos:**

- **Development** → lee `config/database-development.json`
  ```json
  {
    "Database": {
      "Server": "SIMICS-BAYUNCA\\DB_SIMICSGROUP",
      "Database": "GestLogDB_DEV",
      "Username": "sa",
      "Password": "AndresC89@"
    }
  }
  ```

- **Production** → lee `config/database-production.json`
  ```json
  {
    "Database": {
      "Server": "SIMICS-BAYUNCA\\DB_SIMICSGROUP",
      "Database": "GestLogDB",
      "Username": "sa",
      "Password": "AndresC89@"
    }
  }
  ```

---

## 🔍 Verificar Qué Ambiente Estoy Usando

**Los logs mostrarán al iniciar:**

```
✅ Entorno detectado: Development
🔗 Leyendo configuración para ambiente: Development
📋 Conectando a: GestLogDB_DEV
```

o

```
✅ Entorno detectado: Production  
🔗 Leyendo configuración para ambiente: Production
📋 Conectando a: GestLogDB
```

---

## ✅ Diferencias Automáticas

## ✅ Diferencias Automáticas

| Aspecto | Development | Production |
|--------|-------------|-----------|
| **BD** | GestLogDB_DEV | GestLogDB |
| **Usuario Test** | admin/admin123 | (reales) |
| **Actualizaciones** | ❌ Deshabilitadas | ✅ Habilitadas |
| **Backups** | ❌ No | ✅ Sí |
| **Propósito** | Desarrollo/Pruebas | Producción |

---

## ⚠️ Lo Único Manual es la Variable

✅ **Solo esto es manual:**
```powershell
$env:GESTLOG_ENVIRONMENT = "Development"  # o "Production"
```

✅ **Todo lo demás es automático:**
- ✨ Detecta el archivo de configuración
- ✨ Lee credenciales de BD
- ✨ Conecta a la BD correcta
- ✨ Carga configuración de actualizaciones
- ✨ Sincroniza variables de entorno

---

## 🆘 Troubleshooting

### **Problema: Sigue conectando a BD anterior**

**Solución:**
```powershell
# Abre una nueva terminal PowerShell
# Establece la variable nuevamente
$env:GESTLOG_ENVIRONMENT = "Development"

# Ejecuta
dotnet run --launch-profile Development
```

### **Problema: "Usuario no encontrado"**

**Causa:** Estás en Development pero intentando usar usuarios de Production

**Solución:**
```powershell
# En Development usa:
Usuario: admin
Contraseña: admin123

# Verifica en qué BD estás
sqlcmd -S "SIMICS-BAYUNCA\DB_SIMICSGROUP" -U sa -P "AndresC89@" -d GestLogDB_DEV -Q "SELECT COUNT(*) FROM Usuarios"
```

---

## 📝 Configuración (Ahora Simplificada)

### **launchSettings.json** (Solo variable de ambiente)

```json
{
  "profiles": {
    "Development": {
      "environmentVariables": {
        "GESTLOG_ENVIRONMENT": "Development"  // ← Solo esto
      }
    },
    "Production": {
      "environmentVariables": {
        "GESTLOG_ENVIRONMENT": "Production"  // ← Solo esto
      }
    }
  }
}
```

### **database-development.json** (Se lee automáticamente)

```json
{
  "Database": {
    "Server": "SIMICS-BAYUNCA\\DB_SIMICSGROUP",
    "Database": "GestLogDB_DEV",
    "Username": "sa",
    "Password": "AndresC89@"
  }
}
```

---

## 🎯 Quick Start

```powershell
# 1. Development
dotnet run --launch-profile Development
# → Conecta automáticamente a GestLogDB_DEV

# 2. Production  
dotnet run --launch-profile Production
# → Conecta automáticamente a GestLogDB
```

**¡Eso es todo! No hay que configurar nada más.** ✨

---

**Última actualización**: 6 de noviembre de 2025  
**Versión**: 2.0 - Fully Automated Environment Detection
