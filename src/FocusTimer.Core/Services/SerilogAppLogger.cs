namespace FocusTimer.Core.Services
{
    using System;
    using FocusTimer.Core.Interfaces;
    using Serilog;

    /// <summary>
    /// Wraps Serilog logger to implement IAppLogger interface.
    /// Responsible for diagnostic and application logging only (not data persistence).
    /// Follows Dependency Inversion: depends on ILogger abstraction, not concrete Serilog.
    /// </summary>
    public class SerilogAppLogger : IAppLogger, IDisposable
    {
        private const string MessageTemplate = "{Message}";
        private readonly ILogger _logger;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogAppLogger"/> class.
        /// </summary>
        /// <param name="logger">The Serilog logger instance to wrap.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
        public SerilogAppLogger(ILogger logger)
        {
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public void LogDebug(string message)
        {
            this._logger.Debug(MessageTemplate, message);
        }

        /// <inheritdoc/>
        public void LogInformation(string message)
        {
            this._logger.Information(MessageTemplate, message);
        }

        /// <inheritdoc/>
        public void LogWarning(string message)
        {
            this._logger.Warning(MessageTemplate, message);
        }

        /// <inheritdoc/>
        public void LogError(string message, Exception? ex = null)
        {
            if (ex == null)
            {
                this._logger.Error(MessageTemplate, message);
            }
            else
            {
                this._logger.Error(ex, MessageTemplate, message);
            }
        }

        /// <inheritdoc/>
        public void LogCritical(string message, Exception? ex = null)
        {
            if (ex == null)
            {
                this._logger.Fatal(MessageTemplate, message);
            }
            else
            {
                this._logger.Fatal(ex, MessageTemplate, message);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Flushes and closes the logging pipeline.
        /// </summary>
        /// <param name="disposing">True when called from <see cref="Dispose()"/>; false when called from a finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this._disposed)
            {
                return;
            }

            this._disposed = true;

            if (!disposing)
            {
                return;
            }

            try
            {
                this._logger.Information("Application logger shutting down.");
                Log.CloseAndFlush();
            }
            catch
            {
                // Suppress exceptions during shutdown to prevent Dispose from throwing.
                // Logging infrastructure failure should not crash the application cleanup.
            }
        }
    }
}
