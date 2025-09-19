using GestLog.Modules.GestionEquiposInformaticos.Models.Dtos;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using GestLog.Modules.DatabaseConnection;
using MessageBox = System.Windows.MessageBox;

namespace GestLog.Views.Tools.GestionEquipos
{    /// <summary>
    /// ViewModel para el diálogo de periféricos
    /// </summary>
    public partial class PerifericoDialogViewModel : ObservableObject
    {
        [ObservableProperty]
        private PerifericoEquipoInformaticoDto perifericoActual = new();

        [ObservableProperty]
        private string tituloDialog = "Agregar Periférico";

        [ObservableProperty]
        private string textoBotonPrincipal = "Guardar";        // Propiedades para ComboBox con filtro de Usuario Asignado
        [ObservableProperty]
        private ObservableCollection<PersonaConEquipoDto> personasConEquipoDisponibles = new();

        [ObservableProperty]
        private PersonaConEquipoDto? personaConEquipoSeleccionada;

        [ObservableProperty]
        private string filtroUsuarioAsignado = string.Empty;

        [ObservableProperty]
        private ObservableCollection<PersonaConEquipoDto> personasConEquipoFiltradas = new();

        // Variable para suprimir cambios automáticos durante la sincronización
        private bool _suppressFiltroUsuarioChanged = false;

        public List<EstadoPeriferico> EstadosDisponibles { get; } = Enum.GetValues<EstadoPeriferico>().ToList();
        public List<SedePeriferico> SedesDisponibles { get; } = Enum.GetValues<SedePeriferico>().ToList();

        public bool DialogResult { get; private set; }        public PerifericoDialogViewModel()
        {
            // Configurar valores por defecto para un nuevo periférico
            PerifericoActual.FechaCompra = DateTime.Now;
            PerifericoActual.Estado = EstadoPeriferico.EnUso;
            PerifericoActual.Sede = SedePeriferico.AdministrativaBarranquilla;
            PerifericoActual.Costo = 0; // Inicializar en 0 para que el usuario ingrese el valor
            
            // Configurar filtrado reactivo
            PropertyChanged += OnPropertyChanged;
        }private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FiltroUsuarioAsignado))
            {
                OnFiltroUsuarioAsignadoChanged();
            }
            else if (e.PropertyName == nameof(PersonaConEquipoSeleccionada))
            {
                OnPersonaConEquipoSeleccionadaChanged();
            }
        }        /// <summary>
        /// Se ejecuta automáticamente cuando cambia la selección del usuario
        /// Actualiza UsuarioAsignado, CodigoEquipoAsignado y FiltroUsuarioAsignado cuando se selecciona un elemento de la lista
        /// </summary>
        private void OnPersonaConEquipoSeleccionadaChanged()
        {
            System.Diagnostics.Debug.WriteLine($"🔄 OnPersonaConEquipoSeleccionadaChanged -> nuevo={PersonaConEquipoSeleccionada?.NombreCompleto ?? "(null)"}");
            
            if (PersonaConEquipoSeleccionada != null)
            {
                // Capturar la referencia local para evitar problemas de concurrencia
                var personaSeleccionada = PersonaConEquipoSeleccionada;
                
                // IMPORTANTE: Asignar INMEDIATAMENTE a PerifericoActual para mantener los datos estables
                PerifericoActual.UsuarioAsignado = personaSeleccionada.NombreCompleto;
                PerifericoActual.CodigoEquipoAsignado = personaSeleccionada.CodigoEquipo;
                
                System.Diagnostics.Debug.WriteLine($"✅ [INMEDIATO] Usuario asignado: {PerifericoActual.UsuarioAsignado}");
                System.Diagnostics.Debug.WriteLine($"✅ [INMEDIATO] Equipo asignado: {PerifericoActual.CodigoEquipoAsignado}");
                
                // Deferimos la asignación del texto para evitar que el ciclo interno de actualización del ComboBox lo sobrescriba
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        _suppressFiltroUsuarioChanged = true;
                        FiltroUsuarioAsignado = personaSeleccionada.NombreCompleto; // Usar la referencia local
                        _suppressFiltroUsuarioChanged = false;
                        System.Diagnostics.Debug.WriteLine($"🔄 FiltroUsuarioAsignado sincronizado = {FiltroUsuarioAsignado}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error en dispatcher: {ex.Message}");
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            else
            {
                // Solo limpiar el código de equipo, mantener el usuario si hay texto en el filtro
                if (string.IsNullOrWhiteSpace(FiltroUsuarioAsignado))
                {
                    PerifericoActual.UsuarioAsignado = string.Empty;
                }
                PerifericoActual.CodigoEquipoAsignado = string.Empty;
                System.Diagnostics.Debug.WriteLine($"🔄 Selección limpiada");
            }
        }/// <summary>
        /// Se ejecuta cuando el usuario escribe en el ComboBox para filtrar usuarios
        /// </summary>
        private void OnFiltroUsuarioAsignadoChanged()
        {
            System.Diagnostics.Debug.WriteLine($"🔍 OnFiltroUsuarioAsignadoChanged -> value='{FiltroUsuarioAsignado}', suppress={_suppressFiltroUsuarioChanged}");
            
            if (_suppressFiltroUsuarioChanged) return;

            var texto = FiltroUsuarioAsignado ?? string.Empty;

            if (PersonaConEquipoSeleccionada == null)
            {
                PerifericoActual.UsuarioAsignado = texto;
                
                // Si no hay selección y el texto está vacío, limpiar también el código del equipo
                if (string.IsNullOrWhiteSpace(texto))
                {
                    PerifericoActual.CodigoEquipoAsignado = string.Empty;
                }
                
                SincronizarSeleccionPorNombre(texto);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(texto))
                {
                    // Usuario borró el texto, limpiar selección
                    PersonaConEquipoSeleccionada = null;
                    PerifericoActual.UsuarioAsignado = "";
                    PerifericoActual.CodigoEquipoAsignado = string.Empty;
                }
                else if (!PersonaConEquipoSeleccionada.NombreCompleto.Equals(texto, StringComparison.OrdinalIgnoreCase))
                {
                    // Usuario cambió el texto, buscar nueva coincidencia
                    PersonaConEquipoSeleccionada = null;
                    PerifericoActual.UsuarioAsignado = texto;
                    PerifericoActual.CodigoEquipoAsignado = string.Empty; // Limpiar código hasta nueva selección válida
                    SincronizarSeleccionPorNombre(texto);
                }
            }

            FiltrarPersonasConEquipo();
        }

        /// <summary>
        /// Busca y selecciona una persona por nombre completo
        /// </summary>
        private void SincronizarSeleccionPorNombre(string nombreCompleto)
        {
            if (string.IsNullOrWhiteSpace(nombreCompleto)) return;

            var encontrada = PersonasConEquipoDisponibles.FirstOrDefault(p =>
                p.NombreCompleto.Equals(nombreCompleto.Trim(), StringComparison.OrdinalIgnoreCase));

            if (encontrada != null)
            {
                System.Diagnostics.Debug.WriteLine($"🎯 Sincronización automática: encontrada persona {encontrada.NombreCompleto}");
                PersonaConEquipoSeleccionada = encontrada;
            }
        }public void ConfigurarParaEdicion(PerifericoEquipoInformaticoDto periferico)
        {
            PerifericoActual = new PerifericoEquipoInformaticoDto
            {
                Id = periferico.Id,
                Codigo = periferico.Codigo,
                Dispositivo = periferico.Dispositivo,
                FechaCompra = periferico.FechaCompra,
                Costo = periferico.Costo,
                Marca = periferico.Marca,
                Modelo = periferico.Modelo,
                Serial = periferico.Serial,
                UsuarioAsignado = periferico.UsuarioAsignado,
                CodigoEquipoAsignado = periferico.CodigoEquipoAsignado,
                Sede = periferico.Sede,
                Estado = periferico.Estado,
                Observaciones = periferico.Observaciones
            };            TituloDialog = "Editar Periférico";
            TextoBotonPrincipal = "Actualizar";
            
            // Buscar y seleccionar la persona con equipo correspondiente si existe
            _ = Task.Run(async () => await BuscarPersonaConEquipoExistente(periferico.UsuarioAsignado));
        }        /// <summary>
        /// Carga las personas que tienen equipos asignados con el formato requerido
        /// </summary>
        public async Task CargarPersonasConEquipoAsync()
        {
            try
            {                System.Diagnostics.Debug.WriteLine("🔍 Iniciando carga de personas con equipos...");
                
                var connectionString = GetProductionConnectionString();
                var options = new DbContextOptionsBuilder<GestLogDbContext>()
                    .UseSqlServer(connectionString, sqlOptions => 
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                        sqlOptions.CommandTimeout(60);
                    })
                    .Options;

                using var dbContext = new GestLogDbContext(options);
                  // Primero verificar si hay equipos con usuarios asignados
                var equiposConUsuarios = await dbContext.EquiposInformaticos
                    .Where(e => !string.IsNullOrEmpty(e.UsuarioAsignado) && !string.IsNullOrEmpty(e.NombreEquipo))
                    .Select(e => new { e.UsuarioAsignado, e.Codigo, e.NombreEquipo })
                    .ToListAsync();
                
                System.Diagnostics.Debug.WriteLine($"📊 Encontrados {equiposConUsuarios.Count} equipos con usuarios asignados");
                  // Luego verificar si hay personas activas - construyendo el nombre completo
                var personasActivas = await dbContext.Personas
                    .Where(p => p.Activo && !string.IsNullOrEmpty(p.Nombres) && !string.IsNullOrEmpty(p.Apellidos))
                    .Select(p => new { p.IdPersona, NombreCompleto = (p.Nombres ?? "") + " " + (p.Apellidos ?? "") })
                    .ToListAsync();
                
                System.Diagnostics.Debug.WriteLine($"👥 Encontradas {personasActivas.Count} personas activas");
                
                // Hacer el JOIN manualmente para mejor control
                var personasConEquipos = new List<PersonaConEquipoDto>();
                  foreach (var equipo in equiposConUsuarios)
                {
                    if (string.IsNullOrWhiteSpace(equipo.UsuarioAsignado)) continue;
                    
                    var persona = personasActivas.FirstOrDefault(p => 
                        !string.IsNullOrWhiteSpace(p.NombreCompleto) && 
                        p.NombreCompleto.Trim().Equals(equipo.UsuarioAsignado.Trim(), StringComparison.OrdinalIgnoreCase));
                    
                    if (persona != null)
                    {
                        var dto = new PersonaConEquipoDto
                        {
                            PersonaId = persona.IdPersona,
                            NombreCompleto = persona.NombreCompleto,
                            CodigoEquipo = equipo.Codigo ?? "",
                            NombreEquipo = equipo.NombreEquipo ?? "",
                            TextoNormalizado = NormalizeString($"{persona.NombreCompleto} {equipo.Codigo} {equipo.NombreEquipo}")
                        };
                        personasConEquipos.Add(dto);
                        System.Diagnostics.Debug.WriteLine($"✅ Agregada: {dto.DisplayText}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ No se encontró persona para usuario: '{equipo.UsuarioAsignado}'");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"🎯 Total personas con equipos: {personasConEquipos.Count}");

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    PersonasConEquipoDisponibles.Clear();
                    PersonasConEquipoFiltradas.Clear();
                    
                    foreach (var persona in personasConEquipos.OrderBy(p => p.NombreCompleto))
                    {
                        PersonasConEquipoDisponibles.Add(persona);
                        PersonasConEquipoFiltradas.Add(persona);
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"🔄 Actualizada UI con {PersonasConEquipoFiltradas.Count} personas");
                });
            }
            catch (Exception ex)
            {
                // Log error pero no romper la UI
                System.Diagnostics.Debug.WriteLine($"❌ Error al cargar personas con equipos: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Mostrar error al usuario
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Error al cargar usuarios con equipos: {ex.Message}", 
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
        }        /// <summary>
        /// Busca y selecciona una persona con equipo basada en el nombre de usuario y código de equipo existentes
        /// </summary>
        private async Task BuscarPersonaConEquipoExistente(string? usuarioAsignado)
        {
            if (string.IsNullOrWhiteSpace(usuarioAsignado)) return;

            await CargarPersonasConEquipoAsync();

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var objetivo = NormalizeString(usuarioAsignado);
                var codigoEquipoAsignado = PerifericoActual.CodigoEquipoAsignado;
                
                PersonaConEquipoDto? encontrada = null;
                
                // Primero intentar buscar por usuario Y código de equipo (coincidencia exacta)
                if (!string.IsNullOrWhiteSpace(codigoEquipoAsignado))
                {
                    encontrada = PersonasConEquipoDisponibles.FirstOrDefault(p => 
                        NormalizeString(p.NombreCompleto) == objetivo && 
                        p.CodigoEquipo.Equals(codigoEquipoAsignado, StringComparison.OrdinalIgnoreCase));
                    
                    if (encontrada != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"🎯 Coincidencia exacta: Usuario '{encontrada.NombreCompleto}' + Equipo '{encontrada.CodigoEquipo}'");
                    }
                }
                
                // Si no se encontró coincidencia exacta, buscar solo por usuario
                if (encontrada == null)
                {
                    encontrada = PersonasConEquipoDisponibles.FirstOrDefault(p => 
                        NormalizeString(p.NombreCompleto) == objetivo);
                    
                    if (encontrada != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"🎯 Coincidencia por usuario: '{encontrada.NombreCompleto}' (puede tener diferente equipo)");
                    }
                }
                
                // Si aún no se encontró, búsqueda flexible
                if (encontrada == null)
                {
                    encontrada = PersonasConEquipoDisponibles.FirstOrDefault(p =>
                        NormalizeString(p.NombreCompleto).Contains(objetivo) || 
                        objetivo.Contains(NormalizeString(p.NombreCompleto)));
                    
                    if (encontrada != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"🎯 Coincidencia flexible: '{encontrada.NombreCompleto}'");
                    }
                }                if (encontrada != null)
                {
                    PersonaConEquipoSeleccionada = encontrada;
                    FiltroUsuarioAsignado = encontrada.NombreCompleto;
                    
                    // Actualizar también el código del equipo en el periférico actual
                    PerifericoActual.CodigoEquipoAsignado = encontrada.CodigoEquipo;
                    
                    System.Diagnostics.Debug.WriteLine($"✅ Persona existente encontrada y asignada:");
                    System.Diagnostics.Debug.WriteLine($"   - Usuario: {encontrada.NombreCompleto}");
                    System.Diagnostics.Debug.WriteLine($"   - Equipo: {encontrada.CodigoEquipo}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ No se encontró persona para usuario: '{usuarioAsignado}'");
                    FiltroUsuarioAsignado = usuarioAsignado.Trim();
                }
            });
        }/// <summary>
        /// Filtra las personas con equipo basado en el texto de filtro
        /// </summary>
        private void FiltrarPersonasConEquipo()
        {
            if (PersonasConEquipoDisponibles == null) return;

            PersonasConEquipoFiltradas.Clear();

            if (string.IsNullOrWhiteSpace(FiltroUsuarioAsignado))
            {
                foreach (var persona in PersonasConEquipoDisponibles)
                {
                    PersonasConEquipoFiltradas.Add(persona);
                }
                return;
            }

            var filtroNormalizado = NormalizeString(FiltroUsuarioAsignado);
            var personasFiltradas = PersonasConEquipoDisponibles
                .Where(p => p.TextoNormalizado.Contains(filtroNormalizado))
                .ToList();

            foreach (var persona in personasFiltradas)
            {
                PersonasConEquipoFiltradas.Add(persona);
            }

            System.Diagnostics.Debug.WriteLine($"🔍 Filtradas {PersonasConEquipoFiltradas.Count} personas con texto '{FiltroUsuarioAsignado}'");
        }

        /// <summary>
        /// Normaliza un string para filtrado (sin acentos, minúsculas)
        /// </summary>
        private static string NormalizeString(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            
            var normalized = input.Normalize(NormalizationForm.FormD);
            var chars = normalized.Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark).ToArray();
            return new string(chars).Normalize(NormalizationForm.FormC).Trim().ToLowerInvariant();
        }        /// <summary>
        /// Obtiene la cadena de conexión de producción
        /// </summary>
        private static string GetProductionConnectionString()
        {
            return "Server=SIMICSGROUPWKS1\\SIMICSBD;Database=BD_ Pruebas;User Id=sa;Password=S1m1cS!DB_2025;TrustServerCertificate=true;Connection Timeout=30;";
        }

        /// <summary>
        /// Método de prueba para verificar la conexión y datos
        /// </summary>
        public async Task ProbarConexionYDatosAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔌 Probando conexión a base de datos...");
                  var connectionString = GetProductionConnectionString();
                var options = new DbContextOptionsBuilder<GestLogDbContext>()
                    .UseSqlServer(connectionString, sqlOptions => 
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                        sqlOptions.CommandTimeout(60);
                    })
                    .Options;

                using var dbContext = new GestLogDbContext(options);
                
                // Probar conexión
                var canConnect = await dbContext.Database.CanConnectAsync();
                System.Diagnostics.Debug.WriteLine($"📡 Conexión DB: {(canConnect ? "✅ OK" : "❌ FALLO")}");
                
                if (canConnect)
                {
                    // Contar equipos totales
                    var totalEquipos = await dbContext.EquiposInformaticos.CountAsync();
                    System.Diagnostics.Debug.WriteLine($"📦 Total equipos: {totalEquipos}");
                    
                    // Contar personas totales
                    var totalPersonas = await dbContext.Personas.CountAsync();
                    System.Diagnostics.Debug.WriteLine($"👥 Total personas: {totalPersonas}");
                    
                    // Algunos ejemplos de equipos con usuarios
                    var ejemplosEquipos = await dbContext.EquiposInformaticos
                        .Where(e => !string.IsNullOrEmpty(e.UsuarioAsignado))
                        .Take(5)
                        .Select(e => new { e.Codigo, e.UsuarioAsignado, e.NombreEquipo })
                        .ToListAsync();
                    
                    System.Diagnostics.Debug.WriteLine($"📋 Ejemplos de equipos con usuarios ({ejemplosEquipos.Count}):");
                    foreach (var eq in ejemplosEquipos)
                    {
                        System.Diagnostics.Debug.WriteLine($"   - {eq.Codigo}: {eq.UsuarioAsignado} | {eq.NombreEquipo}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en prueba de conexión: {ex.Message}");
            }
        }        [RelayCommand]
        private void Guardar()
        {
            System.Diagnostics.Debug.WriteLine("🔄 Iniciando proceso de guardado...");
            
            // AGREGADO: Logs detallados de depuración
            System.Diagnostics.Debug.WriteLine($"🔍 [DEBUG] Estado antes de asignación:");
            System.Diagnostics.Debug.WriteLine($"🔍 [DEBUG] - PersonaConEquipoSeleccionada: {(PersonaConEquipoSeleccionada != null ? PersonaConEquipoSeleccionada.NombreCompleto : "NULL")}");
            System.Diagnostics.Debug.WriteLine($"🔍 [DEBUG] - FiltroUsuarioAsignado: '{FiltroUsuarioAsignado}'");
            System.Diagnostics.Debug.WriteLine($"🔍 [DEBUG] - PerifericoActual.UsuarioAsignado actual: '{PerifericoActual.UsuarioAsignado}'");
            System.Diagnostics.Debug.WriteLine($"🔍 [DEBUG] - PerifericoActual.CodigoEquipoAsignado actual: '{PerifericoActual.CodigoEquipoAsignado}'");
            
            // IMPORTANTE: Asignar los valores ANTES de la validación para asegurar que están disponibles
            if (PersonaConEquipoSeleccionada != null)
            {
                PerifericoActual.UsuarioAsignado = PersonaConEquipoSeleccionada.NombreCompleto;
                PerifericoActual.CodigoEquipoAsignado = PersonaConEquipoSeleccionada.CodigoEquipo;
                System.Diagnostics.Debug.WriteLine($"✅ Asignado usuario: {PerifericoActual.UsuarioAsignado}");
                System.Diagnostics.Debug.WriteLine($"✅ Asignado código equipo: {PerifericoActual.CodigoEquipoAsignado}");
            }
            else
            {
                // MEJORADO: Mejor lógica de fallback
                if (!string.IsNullOrWhiteSpace(FiltroUsuarioAsignado))
                {
                    // Intentar encontrar una coincidencia exacta en la lista disponible
                    var personaEncontrada = PersonasConEquipoDisponibles?.FirstOrDefault(p => 
                        p.NombreCompleto.Equals(FiltroUsuarioAsignado.Trim(), StringComparison.OrdinalIgnoreCase));
                    
                    if (personaEncontrada != null)
                    {
                        // Encontramos una coincidencia exacta, usar esos datos
                        PerifericoActual.UsuarioAsignado = personaEncontrada.NombreCompleto;
                        PerifericoActual.CodigoEquipoAsignado = personaEncontrada.CodigoEquipo;
                        System.Diagnostics.Debug.WriteLine($"🎯 Encontrada coincidencia exacta: {personaEncontrada.NombreCompleto} / {personaEncontrada.CodigoEquipo}");
                    }
                    else
                    {
                        // No hay coincidencia exacta, usar solo el texto del filtro como usuario
                        PerifericoActual.UsuarioAsignado = FiltroUsuarioAsignado.Trim();
                        PerifericoActual.CodigoEquipoAsignado = null; // Sin código de equipo específico
                        System.Diagnostics.Debug.WriteLine($"📝 Usando solo texto del filtro: {PerifericoActual.UsuarioAsignado}");
                    }
                }
                else
                {
                    // No hay nada seleccionado ni escrito, limpiar los campos
                    PerifericoActual.UsuarioAsignado = null;
                    PerifericoActual.CodigoEquipoAsignado = null;
                    System.Diagnostics.Debug.WriteLine("🧹 No hay datos, campos de asignación limpiados");
                }
            }
            
            // AGREGADO: Log final antes de validación
            System.Diagnostics.Debug.WriteLine($"🔍 [DEBUG] Estado FINAL antes de validación:");
            System.Diagnostics.Debug.WriteLine($"🔍 [DEBUG] - PerifericoActual.UsuarioAsignado FINAL: '{PerifericoActual.UsuarioAsignado}'");
            System.Diagnostics.Debug.WriteLine($"🔍 [DEBUG] - PerifericoActual.CodigoEquipoAsignado FINAL: '{PerifericoActual.CodigoEquipoAsignado}'");
            
            if (ValidarFormulario())
            {
                System.Diagnostics.Debug.WriteLine("✅ Validación exitosa, estableciendo DialogResult = true");
                DialogResult = true;
                System.Diagnostics.Debug.WriteLine("🚪 DialogResult establecido, el diálogo debería cerrarse");
                
                // Notificar al Window que cierre el diálogo
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // Buscar el Window padre que contiene este ViewModel
                    if (System.Windows.Application.Current.Windows.Cast<Window>()
                        .FirstOrDefault(w => w.DataContext == this) is PerifericoDialog dialog)
                    {
                        dialog.DialogResult = true;
                        dialog.Close();
                    }
                });
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ Validación falló, el diálogo permanece abierto");            }
        }
        
        private bool ValidarFormulario()
        {
            var errores = new List<string>();

            if (string.IsNullOrWhiteSpace(PerifericoActual.Codigo))
                errores.Add("El código es obligatorio");

            if (string.IsNullOrWhiteSpace(PerifericoActual.Dispositivo))
                errores.Add("El dispositivo es obligatorio");

            if (PerifericoActual.FechaCompra == default)
                errores.Add("La fecha de compra es obligatoria");

            if (PerifericoActual.Costo < 0)
                errores.Add("El costo no puede ser negativo");

            if (errores.Any())
            {
                var mensaje = "Por favor corrija los siguientes errores:\n\n" + string.Join("\n", errores);
                MessageBox.Show(mensaje, "Errores de Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Code-behind para el diálogo de periféricos
    /// </summary>
    public partial class PerifericoDialog : Window
    {
        public PerifericoDialogViewModel ViewModel { get; }
        
        public PerifericoDialog()
        {
            InitializeComponent();
            ViewModel = new PerifericoDialogViewModel();
            DataContext = ViewModel;
            
            // Cargar personas con equipos al inicializar
            Loaded += async (s, e) => 
            {
                System.Diagnostics.Debug.WriteLine("🚀 Iniciando carga del diálogo de periféricos...");
                await ViewModel.ProbarConexionYDatosAsync();
                await ViewModel.CargarPersonasConEquipoAsync();
            };
        }

        public PerifericoDialog(PerifericoEquipoInformaticoDto perifericoParaEditar) : this()
        {
            ViewModel.ConfigurarParaEdicion(perifericoParaEditar);
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (ViewModel.DialogResult)
            {
                DialogResult = true;
            }
            base.OnClosing(e);
        }
    }
}
