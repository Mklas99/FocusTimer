using Serilog;
using FocusTimer.Core.Interfaces;
using System;

namespace FocusTimer.Core.Services
{
    public class SerilogLogWriter : ILogWriter
    {
        private readonly ILogger _logger;

        public SerilogLogWriter(ILogger logger)
        {
            _logger = logger;
        }

        public void LogDebug(string message) => _logger.Debug(message);
        public void LogInformation(string message) => _logger.Information(message);
        public void LogWarning(string message) => _logger.Warning(message);
        public void LogError(string message, Exception? ex = null)
        {
            if (ex != null)
                _logger.Error(ex, message);
            else
                _logger.Error(message);
        }
        public void LogCritical(string message, Exception? ex = null)
        {
            if (ex != null)
                _logger.Fatal(ex, message);
            else
                _logger.Fatal(message);
        }

        public async Task WriteEntriesAsync(IEnumerable<FocusTimer.Core.Models.TimeEntry> entries, FocusTimer.Core.Models.Settings settings)
        {
            // Example: Log each entry as Information
            foreach (var entry in entries)
            {
                _logger.Information($"TimeEntry: {entry.StartTime} - {entry.EndTime}, App: {entry.AppName}, Window: {entry.WindowTitle}, Project: {entry.ProjectTag}");
            }
            await Task.CompletedTask;
        }
    }
}
