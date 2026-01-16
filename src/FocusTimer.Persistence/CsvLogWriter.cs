using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

namespace FocusTimer.Persistence;

/// <summary>
/// Writes time entries to CSV log files organized by date.
/// File structure: {LogDirectory}/{YYYY}/{MM}/{YYYY-MM-DD}-worklog.csv
/// </summary>
public class CsvLogWriter : ILogWriter
{
    public void LogDebug(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[DEBUG] {message}");
    }

    public void LogInformation(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[INFO] {message}");
    }

    public void LogWarning(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[WARN] {message}");
    }

    public void LogError(string message, Exception? ex = null)
    {
        System.Diagnostics.Debug.WriteLine($"[ERROR] {message} " + (ex != null ? ex.ToString() : ""));
    }

    public void LogCritical(string message, Exception? ex = null)
    {
        System.Diagnostics.Debug.WriteLine($"[CRITICAL] {message} " + (ex != null ? ex.ToString() : ""));
    }
    private const string CsvHeader = "Date,StartTime,EndTime,DurationSeconds,AppName,WindowTitle,ProjectTag";

    public async Task WriteEntriesAsync(IEnumerable<TimeEntry> entries, Settings settings)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        var entriesList = entries?.ToList() ?? new List<TimeEntry>();
        if (!entriesList.Any())
        {
            System.Diagnostics.Debug.WriteLine("No entries to write");
            return;
        }

        // Validate log directory
        if (string.IsNullOrWhiteSpace(settings.LogDirectory))
        {
            System.Diagnostics.Debug.WriteLine("Log directory is not configured");
            return;
        }

        try
        {
            // Group entries by date (in case session spans midnight)
            var entriesByDate = entriesList.GroupBy(e => e.StartTime.Date);

            foreach (var dateGroup in entriesByDate)
            {
                var date = dateGroup.Key;
                var logFilePath = GetLogFilePath(settings.LogDirectory, date);

                try
                {
                    await WriteEntriesToFileAsync(logFilePath, dateGroup);
                    System.Diagnostics.Debug.WriteLine($"Wrote {dateGroup.Count()} entries to {logFilePath}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to write to {logFilePath}: {ex.Message}");
                    // Continue trying to write other dates
                }
            }
        }
        catch (Exception ex)
        {
            // Log error but don't crash
            System.Diagnostics.Debug.WriteLine($"Failed to write log entries: {ex.Message}");
            // TODO: Consider showing a notification to the user
            throw; // Re-throw so caller knows it failed
        }
    }

    /// <summary>
    /// Writes a group of entries to a specific file.
    /// </summary>
    private async Task WriteEntriesToFileAsync(string logFilePath, IEnumerable<TimeEntry> entries)
    {
        // Ensure directory exists
        var directory = Path.GetDirectoryName(logFilePath);
        if (string.IsNullOrEmpty(directory))
            throw new InvalidOperationException($"Invalid log file path: {logFilePath}");

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Check if file exists; if not, write header
        bool fileExists = File.Exists(logFilePath);

        // Use FileStream with proper sharing to avoid conflicts
        using var fileStream = new FileStream(
            logFilePath,
            FileMode.Append,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);

        using var writer = new StreamWriter(fileStream, Encoding.UTF8);

        if (!fileExists)
        {
            await writer.WriteLineAsync(CsvHeader);
        }

        foreach (var entry in entries)
        {
            // Skip invalid entries
            if (entry.StartTime == default || entry.EndTime == null)
            {
                System.Diagnostics.Debug.WriteLine($"Skipping invalid entry: StartTime={entry.StartTime}, EndTime={entry.EndTime}");
                continue;
            }

            var csvLine = FormatCsvLine(entry);
            await writer.WriteLineAsync(csvLine);
        }

        await writer.FlushAsync();
    }

    /// <summary>
    /// Determines the log file path for a given date.
    /// Format: {LogDirectory}/{YYYY}/{MM}/{YYYY-MM-DD}-worklog.csv
    /// </summary>
    private string GetLogFilePath(string logDirectory, DateTime date)
    {
        var year = date.Year.ToString("D4", CultureInfo.InvariantCulture);
        var month = date.Month.ToString("D2", CultureInfo.InvariantCulture);
        var dateStr = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var fileName = $"{dateStr}-worklog.csv";

        return Path.Combine(logDirectory, year, month, fileName);
    }

    /// <summary>
    /// Formats a TimeEntry as a CSV line with proper escaping.
    /// </summary>
    private string FormatCsvLine(TimeEntry entry)
    {
        var date = entry.StartTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var startTime = entry.StartTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        var endTime = entry.EndTime?.ToString("HH:mm:ss", CultureInfo.InvariantCulture) ?? "";
        var durationSeconds = entry.Duration?.TotalSeconds.ToString("F0", CultureInfo.InvariantCulture) ?? "0";
        var appName = EscapeCsvField(entry.AppName);
        var windowTitle = EscapeCsvField(entry.WindowTitle);
        var projectTag = EscapeCsvField(entry.ProjectTag ?? "");

        return $"{date},{startTime},{endTime},{durationSeconds},{appName},{windowTitle},{projectTag}";
    }

    /// <summary>
    /// Escapes a CSV field by wrapping in quotes if it contains special characters.
    /// </summary>
    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        // Wrap in quotes if contains comma, quote, or newline
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            // Escape quotes by doubling them
            var escaped = field.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        return field;
    }

}
