using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

namespace FocusTimer.Persistence
{
    public class CsvSessionRepository : ISessionRepository
    {
        private readonly ISettingsProvider _settingsProvider;
        private readonly IAppLogger? _logger;
        private const string CsvHeader = "Date,StartTime,EndTime,DurationSeconds,AppName,WindowTitle,ProjectTag";
        private DateTime _lastRetentionCleanupDate = DateTime.MinValue;

        public CsvSessionRepository(ISettingsProvider settingsProvider, IAppLogger? logger = null)
        {
            _settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
            _logger = logger;
        }

        public async Task SaveSessionAsync(IEnumerable<TimeEntry> entries)
        {
            try
            {
                var settings = await _settingsProvider.LoadAsync();
                var worklogDirectory = ResolveWorklogDirectory(settings);
                if (string.IsNullOrWhiteSpace(worklogDirectory))
                {
                    _logger?.LogWarning("Worklog directory is not configured. Session entries will not be saved.");
                    return;
                }

                await EnforceRetentionPolicyAsync(settings, worklogDirectory);

                var entriesList = entries?.ToList() ?? new List<TimeEntry>();
                if (!entriesList.Any())
                {
                    _logger?.LogDebug("No session entries to save.");
                    return;
                }

                _logger?.LogInformation($"Saving {entriesList.Count} session entries to {worklogDirectory}");

                var entriesByDate = entriesList.GroupBy(e => e.StartTime.Date);

                foreach (var dateGroup in entriesByDate)
                {
                    var date = dateGroup.Key;
                    var logFilePath = GetLogFilePath(worklogDirectory, date);
                    try
                    {
                        await WriteEntriesToFileAsync(logFilePath, dateGroup);
                        _logger?.LogDebug($"Saved {dateGroup.Count()} entries to {logFilePath}");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError($"Failed to save session entries to {logFilePath}", ex);
                        throw;
                    }
                }

                _logger?.LogInformation($"Successfully saved all session entries.");
            }
            catch (Exception ex)
            {
                _logger?.LogError("Failed to save session entries", ex);
                throw;
            }
        }

        public async Task<IEnumerable<TimeEntry>> GetSessionsByDateAsync(DateTime date)
        {
            try
            {
                var settings = await _settingsProvider.LoadAsync();
                var worklogDirectory = ResolveWorklogDirectory(settings);
                if (string.IsNullOrWhiteSpace(worklogDirectory))
                {
                    _logger?.LogWarning("Worklog directory is not configured. Cannot read session entries.");
                    return Enumerable.Empty<TimeEntry>();
                }

                var logFilePath = GetLogFilePath(worklogDirectory, date);
                if (!File.Exists(logFilePath))
                {
                    _logger?.LogDebug($"No session file found for date {date:yyyy-MM-dd}");
                    return Enumerable.Empty<TimeEntry>();
                }

                var entries = new List<TimeEntry>();
                string[] lines;
                try
                {
                    lines = await File.ReadAllLinesAsync(logFilePath);
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Failed to read session file {logFilePath}", ex);
                    return Enumerable.Empty<TimeEntry>();
                }

                // Skip header
                foreach (var line in lines.Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        var entry = ParseCsvLine(line);
                        if (entry != null) entries.Add(entry);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError($"Failed to parse session line: {line}", ex);
                    }
                }

                _logger?.LogDebug($"Loaded {entries.Count} session entries for {date:yyyy-MM-dd}");
                return entries;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to get sessions for date {date:yyyy-MM-dd}", ex);
                return Enumerable.Empty<TimeEntry>();
            }
        }

        private static string ResolveWorklogDirectory(Settings settings)
        {
            if (!string.IsNullOrWhiteSpace(settings.WorklogDirectory))
            {
                return settings.WorklogDirectory;
            }

            return Settings.DefaultWorklogDirectory;
        }

        private string GetLogFilePath(string logDirectory, DateTime date)
        {
            var year = date.Year.ToString("D4", CultureInfo.InvariantCulture);
            var month = date.Month.ToString("D2", CultureInfo.InvariantCulture);
            var dateStr = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return Path.Combine(logDirectory, year, month, $"{dateStr}-worklog.csv");
        }

        private async Task WriteEntriesToFileAsync(string logFilePath, IEnumerable<TimeEntry> entries)
        {
            var directory = Path.GetDirectoryName(logFilePath);
            if (string.IsNullOrEmpty(directory)) return;

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            bool fileExists = File.Exists(logFilePath);

            // Use FileStream with proper sharing to avoid conflicts
            using var fileStream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, true);
            using var writer = new StreamWriter(fileStream, Encoding.UTF8);

            if (!fileExists)
                await writer.WriteLineAsync(CsvHeader);

            foreach (var entry in entries)
            {
                if (entry.StartTime == default || entry.EndTime == null) continue;
                await writer.WriteLineAsync(FormatCsvLine(entry));
            }
        }

        private async Task EnforceRetentionPolicyAsync(Settings settings, string worklogDirectory)
        {
            if (!Directory.Exists(worklogDirectory))
            {
                return;
            }

            if (settings.DataRetentionDays <= 0)
            {
                return;
            }

            var today = DateTime.Today;
            if (_lastRetentionCleanupDate == today)
            {
                return;
            }

            _lastRetentionCleanupDate = today;
            var cutoff = today.AddDays(-settings.DataRetentionDays);
            var deletedFiles = 0;

            await Task.Run(() =>
            {
                foreach (var filePath in Directory.EnumerateFiles(worklogDirectory, "*-worklog.csv", SearchOption.AllDirectories))
                {
                    var fileName = Path.GetFileName(filePath);
                    if (fileName.Length < 10)
                    {
                        continue;
                    }

                    var datePart = fileName.Substring(0, 10);
                    if (!DateTime.TryParseExact(
                            datePart,
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out var fileDate))
                    {
                        continue;
                    }

                    if (fileDate.Date >= cutoff)
                    {
                        continue;
                    }

                    try
                    {
                        File.Delete(filePath);
                        deletedFiles++;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning($"Failed to delete expired worklog file '{filePath}': {ex.Message}");
                    }
                }

                DeleteEmptyDirectories(worklogDirectory);
            });

            if (deletedFiles > 0)
            {
                _logger?.LogInformation($"Retention cleanup removed {deletedFiles} expired worklog file(s). Cutoff: {cutoff:yyyy-MM-dd}");
            }
        }

        private static void DeleteEmptyDirectories(string rootDirectory)
        {
            foreach (var directory in Directory.EnumerateDirectories(rootDirectory, "*", SearchOption.AllDirectories)
                         .OrderByDescending(path => path.Length))
            {
                if (Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    continue;
                }

                Directory.Delete(directory);
            }
        }

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

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }

       private TimeEntry? ParseCsvLine(string line)
        {
             // Regex matches: "quoted field" OR non-comma-chars
             var pattern = "(?:^|,)(\\\"(?:[^\\\"]+|\\\"\\\")*\\\"|[^,]*)";
             var matches = Regex.Matches(line, pattern);
             
             // We expect 7 columns: Date,StartTime,EndTime,DurationSeconds,AppName,WindowTitle,ProjectTag
             if (matches.Count < 7) return null;

             var values = new string[matches.Count];
             for(int i = 0; i < matches.Count; i++)
             {
                 var val = matches[i].Value;
                 if (val.StartsWith(",")) val = val.Substring(1);
                 
                 if (val.StartsWith("\"") && val.EndsWith("\"")) 
                 {
                     val = val.Substring(1, val.Length - 2).Replace("\"\"", "\"");
                 }
                 values[i] = val;
             }

             // values[0] = Date (yyyy-MM-dd)
             // values[1] = StartTime (HH:mm:ss)
             // values[2] = EndTime (HH:mm:ss)
             // values[3] = Duration
             // values[4] = AppName
             // values[5] = WindowTitle
             // values[6] = ProjectTag

             if (!DateTime.TryParseExact(values[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                 return null;
             
             if (!TimeSpan.TryParse(values[1], CultureInfo.InvariantCulture, out var startTime))
                 return null;

             TimeSpan? endTime = null;
             if (!string.IsNullOrEmpty(values[2]) && TimeSpan.TryParse(values[2], CultureInfo.InvariantCulture, out var et))
             {
                 endTime = et;
             }

             return new TimeEntry
             {
                 StartTime = date.Add(startTime),
                 EndTime = endTime.HasValue ? date.Add(endTime.Value) : null,
                 AppName = values.Length > 4 ? values[4] : "Unknown",
                 WindowTitle = values.Length > 5 ? values[5] : "",
                 ProjectTag = values.Length > 6 ? values[6] : null
             };
        }
    }
}
