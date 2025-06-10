using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.GestionCartera.ViewModels.Base;

/// <summary>
/// ViewModel base para funcionalidades comunes de generación de documentos
/// </summary>
public abstract partial class BaseDocumentGenerationViewModel : ObservableObject, IDisposable
{
    protected readonly IGestLogLogger _logger;
    protected CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty] private string _statusMessage = "Listo";
    [ObservableProperty] private bool _isProcessing = false;
    [ObservableProperty] private bool _isProcessingCompleted = false;
    [ObservableProperty] private double _progressValue = 0;
    [ObservableProperty] private int _totalDocuments = 0;
    [ObservableProperty] private int _currentDocument = 0;

    protected BaseDocumentGenerationViewModel(IGestLogLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected virtual void ResetProgress()
    {
        IsProcessing = false;
        IsProcessingCompleted = false;
        ProgressValue = 0;
        CurrentDocument = 0;
        TotalDocuments = 0;
        StatusMessage = "Listo";
    }

    protected virtual void StartProcessing(string message = "Procesando...")
    {
        IsProcessing = true;
        IsProcessingCompleted = false;
        StatusMessage = message;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    protected virtual void CompleteProcessing(string message = "Completado")
    {
        IsProcessing = false;
        IsProcessingCompleted = true;
        StatusMessage = message;
        ProgressValue = 100;
    }

    public virtual void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        GC.SuppressFinalize(this);
    }
}
