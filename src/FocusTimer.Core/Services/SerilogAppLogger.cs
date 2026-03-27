using System;
using Serilog;
using FocusTimer.Core.Interfaces;

namespace FocusTimer.Core.Services;

/// <summary>
/// Wraps Serilog logger to implement IAppLogger interface.
/// Responsible for diagnostic and application logging only (not data persistence).
/// Follows Dependency Inversion: depends on ILogger abstraction, not concrete Serilog.
/// </summary>
public class SerilogAppLogger : IAppLogger, IDisposable
{
    private readonly ILogger _logger;
    private bool _disposed;

    public SerilogAppLogger(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void LogDebug(string message)
    {
        _logger.Debug("{Message}", message);
    }

    public void LogInformation(string message)
    {
        _logger.Information("{Message}", message);
    }

    public void LogWarning(string message)
    {
        _logger.Warning("{Message}", message);
    }

    public void LogError(string message, Exception? ex = null)
    {
        if (ex == null)
        {
            _logger.Error("{Message}", message);
        }
        else
        {
            _logger.Error(ex, "{Message}", message);
        }
    }

    public void LogCritical(string message, Exception? ex = null)
    {
        if (ex == null)
        {
            _logger.Fatal("{Message}", message);
        }
        else
        {
            _logger.Fatal(ex, "{Message}", message);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _logger.Information("Application logger shutting down.");
            Log.CloseAndFlush();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error closing logger: {ex.Message}");
        }
    }
}
