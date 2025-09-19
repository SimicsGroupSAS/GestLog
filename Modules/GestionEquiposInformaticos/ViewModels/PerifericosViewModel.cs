using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionEquiposInformaticos.Models.Dtos;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;
using GestLog.Modules.DatabaseConnection;
using GestLog.Services.Core.Logging;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using GestLog.Views.Tools.GestionEquipos;

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels
{
    /// <summary>
    /// ViewModel para la gestión de periféricos informáticos
    /// Respeta SRP: solo coordina la gestión CRUD de periféricos
    /// </summary>
    public partial class PerifericosViewModel : ObservableObject
    {
        private readonly IGestLogLogger _logger;

        // AGREGADO: Control de concurrencia para evitar múltiples inicializaciones simultáneas
        private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);
        private bool _isInitialized = false;

        [ObservableProperty]
        private ObservableCollection<PerifericoEquipoInformaticoDto> _perifericos = new();

        [ObservableProperty]
        private PerifericoEquipoInformaticoDto? _perifericoSeleccionado;

        [ObservableProperty]
        private string _filtro = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = "Listo";

        [ObservableProperty]
        private int _totalPerifericos;

        [ObservableProperty]
        private int _perifericosEnUso;

        [ObservableProperty]
        private int _perifericosAlmacenados;

        [ObservableProperty]
        private int _perifericosDadosBaja;

        /// <summary>
        /// Lista de todos los estados disponibles para el ComboBox
        /// </summary>
        public Array EstadosDisponibles { get; } = Enum.GetValues(typeof(EstadoPeriferico));

        /// <summary>
        /// Lista de todas las sedes disponibles para el ComboBox
        /// </summary>
        public Array SedesDisponibles { get; } = Enum.GetValues(typeof(SedePeriferico));

        /// <summary>
        /// Constructor principal con inyección de dependencias
        /// </summary>
        public PerifericosViewModel(IGestLogLogger logger, GestLogDbContext dbContext)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            // CORREGIDO: No usar el DbContext inyectado para evitar problemas de concurrencia
            
            // CORREGIDO: Inicializar automáticamente con control de concurrencia
            _ = Task.Run(async () =>
            {
                try
                {
                    await InicializarAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[PerifericosViewModel] Error al inicializar automáticamente");
                }
            });
        }

        /// <summary>
        /// Inicializa el ViewModel cargando los datos
        /// </summary>
        public async Task InicializarAsync(CancellationToken cancellationToken = default)
        {
            // CORREGIDO: Usar semáforo para evitar inicializaciones concurrentes
            await _initializationSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_isInitialized)
                {
                    _logger.LogInformation("[PerifericosViewModel] Ya está inicializado, omitiendo");
                    return;
                }

                _logger.LogInformation("[PerifericosViewModel] Inicializando gestión de periféricos");
                // CORREGIDO: Llamar al método interno sin semáforo para evitar deadlock
                await CargarPerifericosInternoAsync(cancellationToken);
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PerifericosViewModel] Error al inicializar");
                StatusMessage = "Error al cargar periféricos";
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }

        /// <summary>
        /// Carga todos los periféricos desde la base de datos
        /// </summary>
        [RelayCommand]
        public async Task CargarPerifericosAsync(CancellationToken cancellationToken = default)
        {
            // CORREGIDO: Usar semáforo para evitar cargas concurrentes
            await _initializationSemaphore.WaitAsync(cancellationToken);
            try
            {
                await CargarPerifericosInternoAsync(cancellationToken);
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }

        /// <summary>
        /// Método interno para cargar periféricos sin usar semáforo (para evitar deadlocks)
        /// </summary>
        private async Task CargarPerifericosInternoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Cargando periféricos...";

                _logger.LogInformation("🔍 [DEBUG] CargarPerifericosInternoAsync iniciado");                // CORREGIDO: Crear un DbContext independiente para evitar concurrencia
                var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<GestLogDbContext>()
                    .UseSqlServer(GetConnectionString())
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning))
                    .Options;

                using var dbContext = new GestLogDbContext(options);
                
                var entities = await dbContext.PerifericosEquiposInformaticos
                    .Include(p => p.EquipoAsignado)
                    .OrderBy(p => p.Codigo)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("🔍 [DEBUG] Recuperados {Count} periféricos desde la base de datos", entities.Count);

                Perifericos.Clear();
                foreach (var entity in entities)
                {
                    var dto = ConvertirEntityADto(entity);
                    Perifericos.Add(dto);
                    _logger.LogInformation("🔍 [DEBUG] Agregado periférico: {Codigo} - {Dispositivo}", dto.Codigo ?? "NULL", dto.Dispositivo ?? "NULL");
                }

                ActualizarEstadisticas();
                StatusMessage = $"Cargados {Perifericos.Count} periféricos";
                _logger.LogInformation("[PerifericosViewModel] Cargados {Count} periféricos", Perifericos.Count);
                _logger.LogInformation("🔍 [DEBUG] Lista final en Perifericos.Count: {Count}", Perifericos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PerifericosViewModel] Error al cargar periféricos");
                StatusMessage = "Error al cargar periféricos";
                MessageBox.Show("Error al cargar periféricos. Ver logs para más detalles.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }        /// <summary>
        /// Comando para agregar un nuevo periférico
        /// </summary>
        [RelayCommand]
        public async Task AgregarPerifericoAsync()
        {
            try
            {
                var dialogViewModel = new PerifericoDialogViewModel();
                var dialog = new Views.Tools.GestionEquipos.PerifericoDialog
                {
                    DataContext = dialogViewModel
                };

                if (dialog.ShowDialog() == true)
                {
                    // Obtener el periférico creado desde el ViewModel del diálogo
                    var nuevoPeriferico = dialogViewModel.PerifericoActual;

                    // AGREGADO: Logs de depuración para verificar los datos del diálogo
                    _logger.LogInformation("🔍 [DEBUG] AgregarPerifericoAsync - datos del diálogo:");
                    _logger.LogInformation("🔍 [DEBUG] - Código: '{Codigo}'", nuevoPeriferico.Codigo ?? "NULL");
                    _logger.LogInformation("🔍 [DEBUG] - Dispositivo: '{Dispositivo}'", nuevoPeriferico.Dispositivo ?? "NULL");
                    _logger.LogInformation("🔍 [DEBUG] - Serial: '{Serial}'", nuevoPeriferico.Serial ?? "NULL");
                    _logger.LogInformation("🔍 [DEBUG] - UsuarioAsignado: '{UsuarioAsignado}'", nuevoPeriferico.UsuarioAsignado ?? "NULL");
                    _logger.LogInformation("🔍 [DEBUG] - CodigoEquipoAsignado: '{CodigoEquipoAsignado}'", nuevoPeriferico.CodigoEquipoAsignado ?? "NULL");

                    await GuardarPerifericoAsync(nuevoPeriferico, esNuevo: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PerifericosViewModel] Error al agregar periférico");
                StatusMessage = "Error al agregar periférico";
            }
        }

        /// <summary>
        /// Comando para editar el periférico seleccionado
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanEditarEliminarPeriferico))]
        public async Task EditarPerifericoAsync()
        {
            if (PerifericoSeleccionado == null) return;

            try
            {
                var dialogViewModel = new PerifericoDialogViewModel();
                dialogViewModel.ConfigurarParaEdicion(PerifericoSeleccionado);
                
                var dialog = new Views.Tools.GestionEquipos.PerifericoDialog
                {
                    DataContext = dialogViewModel
                };

                if (dialog.ShowDialog() == true)
                {
                    var perifericoEditado = dialogViewModel.PerifericoActual;
                    await GuardarPerifericoAsync(perifericoEditado, esNuevo: false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PerifericosViewModel] Error al editar periférico");
                StatusMessage = "Error al editar periférico";
            }
        }

        /// <summary>
        /// Comando para eliminar el periférico seleccionado
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanEditarEliminarPeriferico))]
        public async Task EliminarPerifericoAsync()
        {
            if (PerifericoSeleccionado == null) return;

            try
            {
                var resultado = MessageBox.Show(
                    $"¿Está seguro de que desea eliminar el periférico '{PerifericoSeleccionado.Codigo}'?",
                    "Confirmar eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {                // CORREGIDO: Usar DbContext independiente
                var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<GestLogDbContext>()
                    .UseSqlServer(GetConnectionString())
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning))
                    .Options;

                using var dbContext = new GestLogDbContext(options);
                
                var entity = await dbContext.PerifericosEquiposInformaticos
                    .FirstOrDefaultAsync(p => p.Codigo == PerifericoSeleccionado.Codigo);

                    if (entity != null)
                    {
                        dbContext.PerifericosEquiposInformaticos.Remove(entity);
                        await dbContext.SaveChangesAsync();

                        Perifericos.Remove(PerifericoSeleccionado);
                        ActualizarEstadisticas();
                        StatusMessage = "Periférico eliminado exitosamente";
                        _logger.LogInformation("[PerifericosViewModel] Periférico eliminado: {Codigo}", PerifericoSeleccionado.Codigo);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PerifericosViewModel] Error al eliminar periférico");
                StatusMessage = "Error al eliminar periférico";
            }
        }

        /// <summary>
        /// Verifica si se puede editar o eliminar un periférico
        /// </summary>
        public bool CanEditarEliminarPeriferico => PerifericoSeleccionado != null;

        /// <summary>
        /// Guarda un periférico en la base de datos
        /// </summary>
        private async Task GuardarPerifericoAsync(PerifericoEquipoInformaticoDto dto, bool esNuevo)
        {
            try
            {
                // AGREGADO: Logs de depuración para verificar los datos que llegan
                _logger.LogInformation("🔍 [DEBUG] GuardarPerifericoAsync iniciado:");
                _logger.LogInformation("🔍 [DEBUG] - esNuevo: {EsNuevo}", esNuevo);
                _logger.LogInformation("🔍 [DEBUG] - Código: '{Codigo}'", dto.Codigo ?? "NULL");
                _logger.LogInformation("🔍 [DEBUG] - Dispositivo: '{Dispositivo}'", dto.Dispositivo ?? "NULL");
                _logger.LogInformation("🔍 [DEBUG] - Serial: '{Serial}'", dto.Serial ?? "NULL");
                _logger.LogInformation("🔍 [DEBUG] - UsuarioAsignado: '{UsuarioAsignado}'", dto.UsuarioAsignado ?? "NULL");
                _logger.LogInformation("🔍 [DEBUG] - CodigoEquipoAsignado: '{CodigoEquipoAsignado}'", dto.CodigoEquipoAsignado ?? "NULL");                // CORREGIDO: Usar DbContext independiente
                var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<GestLogDbContext>()
                    .UseSqlServer(GetConnectionString())
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning))
                    .Options;

                using var dbContext = new GestLogDbContext(options);

                PerifericoEquipoInformaticoEntity entity;

                if (esNuevo)
                {
                    // Verificar si ya existe un periférico con el mismo código
                    var existe = await dbContext.PerifericosEquiposInformaticos
                        .AnyAsync(p => p.Codigo == dto.Codigo);

                    if (existe)
                    {
                        MessageBox.Show("Ya existe un periférico con ese código.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    entity = new PerifericoEquipoInformaticoEntity();
                    dbContext.PerifericosEquiposInformaticos.Add(entity);
                }
                else
                {
                    // CORREGIDO: Limpiar ChangeTracker para evitar conflictos
                    var tracked = dbContext.ChangeTracker.Entries<PerifericoEquipoInformaticoEntity>()
                        .Where(e => e.Entity.Codigo == dto.Codigo)
                        .ToList();

                    foreach (var trackedEntity in tracked)
                    {
                        dbContext.Entry(trackedEntity.Entity).State = EntityState.Detached;
                    }

                    var existingEntity = await dbContext.PerifericosEquiposInformaticos
                        .FirstOrDefaultAsync(p => p.Codigo == dto.Codigo);

                    if (existingEntity == null)
                    {
                        MessageBox.Show("No se encontró el periférico a actualizar.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    entity = existingEntity;
                }

                // Mapear DTO a Entity
                entity.Codigo = dto.Codigo;
                entity.Dispositivo = dto.Dispositivo;
                entity.FechaCompra = dto.FechaCompra;
                entity.Costo = dto.Costo;
                entity.Marca = dto.Marca;
                entity.Modelo = dto.Modelo;
                entity.SerialNumber = dto.Serial;
                entity.CodigoEquipoAsignado = string.IsNullOrEmpty(dto.CodigoEquipoAsignado) ? null : dto.CodigoEquipoAsignado;
                entity.UsuarioAsignado = string.IsNullOrEmpty(dto.UsuarioAsignado) ? null : dto.UsuarioAsignado;
                entity.Sede = dto.Sede;
                entity.Estado = dto.Estado;
                entity.Observaciones = dto.Observaciones;

                // AGREGADO: Logs después del mapeo para verificar los valores asignados
                _logger.LogInformation("🔍 [DEBUG] Después del mapeo:");
                _logger.LogInformation("🔍 [DEBUG] - entity.SerialNumber: '{SerialNumber}'", entity.SerialNumber ?? "NULL");
                _logger.LogInformation("🔍 [DEBUG] - entity.UsuarioAsignado: '{UsuarioAsignado}'", entity.UsuarioAsignado ?? "NULL");
                _logger.LogInformation("🔍 [DEBUG] - entity.CodigoEquipoAsignado: '{CodigoEquipoAsignado}'", entity.CodigoEquipoAsignado ?? "NULL");

                await dbContext.SaveChangesAsync();

                if (esNuevo)
                {
                    var nuevoDto = ConvertirEntityADto(entity);
                    Perifericos.Add(nuevoDto);
                }
                else
                {
                    // Actualizar el DTO existente
                    var dtoExistente = Perifericos.FirstOrDefault(p => p.Codigo == dto.Codigo);
                    if (dtoExistente != null)
                    {
                        ActualizarDto(dtoExistente, entity);
                    }
                }

                ActualizarEstadisticas();
                StatusMessage = esNuevo ? "Periférico agregado exitosamente" : "Periférico actualizado exitosamente";
                _logger.LogInformation("[PerifericosViewModel] Periférico {Accion}: {Codigo}", esNuevo ? "agregado" : "actualizado", dto.Codigo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PerifericosViewModel] Error al guardar periférico");
                StatusMessage = "Error al guardar periférico";
                MessageBox.Show("Error al guardar el periférico. Ver logs para más detalles.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Convierte una entidad a DTO
        /// </summary>
        private PerifericoEquipoInformaticoDto ConvertirEntityADto(PerifericoEquipoInformaticoEntity entity)
        {
            return new PerifericoEquipoInformaticoDto(entity);
        }

        /// <summary>
        /// Actualiza un DTO existente con datos de la entidad
        /// </summary>
        private void ActualizarDto(PerifericoEquipoInformaticoDto dto, PerifericoEquipoInformaticoEntity entity)
        {
            dto.Dispositivo = entity.Dispositivo;
            dto.FechaCompra = entity.FechaCompra;
            dto.Costo = entity.Costo;
            dto.Marca = entity.Marca;
            dto.Modelo = entity.Modelo;
            dto.Serial = entity.SerialNumber;
            dto.CodigoEquipoAsignado = entity.CodigoEquipoAsignado;
            dto.UsuarioAsignado = entity.UsuarioAsignado;
            dto.Sede = entity.Sede;
            dto.Estado = entity.Estado;
            dto.Observaciones = entity.Observaciones;
            dto.FechaModificacion = entity.FechaModificacion;
            dto.NombreEquipoAsignado = entity.EquipoAsignado?.NombreEquipo;
        }

        /// <summary>
        /// Actualiza las estadísticas mostradas en la vista
        /// </summary>
        private void ActualizarEstadisticas()
        {
            TotalPerifericos = Perifericos.Count;
            PerifericosEnUso = Perifericos.Count(p => p.Estado == EstadoPeriferico.EnUso);
            PerifericosAlmacenados = Perifericos.Count(p => p.Estado == EstadoPeriferico.AlmacenadoFuncionando);
            PerifericosDadosBaja = Perifericos.Count(p => p.Estado == EstadoPeriferico.DadoDeBaja);
        }        /// <summary>
        /// Obtiene la cadena de conexión para crear DbContext independientes
        /// </summary>
        private string GetConnectionString()
        {
            // CORREGIDO: Usar la misma cadena de conexión que funciona en el resto de la aplicación
            return "Server=SIMICSGROUPWKS1\\SIMICSBD;Database=BD_ Pruebas;User Id=sa;Password=S1m1cS!DB_2025;TrustServerCertificate=True;Connection Timeout=30;";
        }
    }
}
