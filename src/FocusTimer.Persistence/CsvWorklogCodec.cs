#pragma warning disable

namespace FocusTimer.Persistence;

using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using FocusTimer.Core.Models;

/// <summary>Reads and writes the single supported version of the worklog CSV schema.</summary>
public sealed class CsvWorklogCodec
{
    /// <summary>The supported row schema version.</summary>
    public const string CurrentSchemaVersion = "1";

    /// <summary>The stable order emitted for every newly written worklog file.</summary>
    public static readonly IReadOnlyList<string> Header =
    [
        "SchemaVersion", "EntryId", "SessionId", "StartedAt", "EndedAt", "DurationSeconds", "AppName",
        "WindowTitle", "ProjectTag", "ProjectAssignmentSource", "ProjectRuleId", "ActivityKind", "EndReason",
        "CaptureSource", "SourcePlatform", "SourceDeviceId", "Revision", "LastModifiedAtUtc",
    ];

    /// <summary>Writes entries using the stable current-schema column order.</summary>
    /// <param name="stream">The destination stream.</param>
    /// <param name="entries">The valid worklog entries to serialize.</param>
    /// <param name="cancellationToken">A token that cancels the write.</param>
    /// <returns>A task that completes after the CSV data has been flushed.</returns>
    public async Task WriteAsync(Stream stream, IEnumerable<TimeEntry> entries, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(entries);
        var records = entries.ToList();
        var errors = records.SelectMany(WorklogEntryValidator.Validate).ToList();
        if (errors.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", errors), nameof(entries));
        }

        await using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
        using var csv = new CsvWriter(writer, CreateConfiguration());
        foreach (var name in Header)
        {
            csv.WriteField(name);
        }

        csv.NextRecord();
        foreach (var entry in records)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WriteEntry(csv, entry);
            csv.NextRecord();
        }

        await writer.FlushAsync(cancellationToken);
    }

    /// <summary>Reads a current-schema CSV stream by header name.</summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="filePath">The source path reported in diagnostics.</param>
    /// <param name="cancellationToken">A token that cancels the read.</param>
    /// <returns>The decoded entries, warnings, or typed read failure.</returns>
    public async Task<CsvWorklogRead> ReadAsync(Stream stream, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        using var reader = new StreamReader(stream, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        using var parser = new CsvParser(reader, CreateConfiguration());
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!parser.Read() || parser.Record is null)
            {
                return CsvWorklogRead.UnsupportedSchema("The worklog header is missing.");
            }

            var index = CreateHeaderMap(parser.Record);
            if (index is null)
            {
                return CsvWorklogRead.UnsupportedSchema("The file does not use the current worklog schema.");
            }

            var entries = new List<TimeEntry>();
            var warnings = new List<WorklogWarning>();
            while (parser.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var record = parser.Record;
                if (record is null || record.Length != Header.Count)
                {
                    warnings.Add(new WorklogWarning(filePath, (int)parser.Row, "The record has an unexpected field count."));
                    continue;
                }

                try
                {
                    entries.Add(ParseEntry(record, index));
                }
                catch (Exception exception) when (exception is FormatException or ArgumentException or OverflowException)
                {
                    warnings.Add(new WorklogWarning(filePath, (int)parser.Row, exception.Message));
                }
            }

            return new CsvWorklogRead(WorklogOutcome.Success(warnings), entries);
        }
        catch (CsvHelperException exception)
        {
            return CsvWorklogRead.Malformed(exception.Message);
        }
        catch (DecoderFallbackException exception)
        {
            return CsvWorklogRead.Malformed(exception.Message);
        }
        catch (InvalidDataException exception)
        {
            return CsvWorklogRead.Malformed(exception.Message);
        }
    }

    private static CsvConfiguration CreateConfiguration() => new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = false,
        BadDataFound = arguments => throw new CsvHelperException(arguments.Context, arguments.Field),
        DetectColumnCountChanges = false,
        IgnoreBlankLines = false,
    };

    private static Dictionary<string, int>? CreateHeaderMap(string[] header)
    {
        if (header.Length != Header.Count)
        {
            return null;
        }

        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < header.Length; i++)
        {
            if (!index.TryAdd(header[i], i))
            {
                return null;
            }
        }

        return Header.All(index.ContainsKey) ? index : null;
    }

    private static void WriteEntry(CsvWriter csv, TimeEntry entry)
    {
        csv.WriteField(CurrentSchemaVersion);
        csv.WriteField(entry.EntryId);
        csv.WriteField(entry.SessionId);
        csv.WriteField(entry.StartedAt.ToString("O", CultureInfo.InvariantCulture));
        csv.WriteField(entry.EndedAt.ToString("O", CultureInfo.InvariantCulture));
        csv.WriteField(entry.Duration.TotalSeconds.ToString(CultureInfo.InvariantCulture));
        csv.WriteField(entry.AppName);
        csv.WriteField(entry.WindowTitle);
        csv.WriteField(entry.ProjectTag ?? string.Empty);
        csv.WriteField(WorklogValueCodec.ToStoredValue(entry.ProjectAssignmentSource));
        csv.WriteField(entry.ProjectRuleId ?? string.Empty);
        csv.WriteField(WorklogValueCodec.ToStoredValue(entry.ActivityKind));
        csv.WriteField(WorklogValueCodec.ToStoredValue(entry.EndReason));
        csv.WriteField(WorklogValueCodec.ToStoredValue(entry.CaptureSource));
        csv.WriteField(WorklogValueCodec.ToStoredValue(entry.SourcePlatform));
        csv.WriteField(entry.SourceDeviceId ?? string.Empty);
        csv.WriteField(entry.Revision.ToString(CultureInfo.InvariantCulture));
        csv.WriteField(entry.LastModifiedAtUtc.ToString("O", CultureInfo.InvariantCulture));
    }

    private static TimeEntry ParseEntry(IReadOnlyList<string> values, IReadOnlyDictionary<string, int> index)
    {
        string Field(string name) => values[index[name]];
        if (!string.Equals(Field("SchemaVersion"), CurrentSchemaVersion, StringComparison.Ordinal))
        {
            throw new FormatException("The record has an unsupported schema version.");
        }

        var entry = new TimeEntry(
            Field("EntryId"), Field("SessionId"), ParseTimestamp(Field("StartedAt")), ParseTimestamp(Field("EndedAt")),
            Field("AppName"), Field("WindowTitle"), Empty(Field("ProjectTag")), ParseProject(Field("ProjectAssignmentSource")),
            Empty(Field("ProjectRuleId")), ParseActivity(Field("ActivityKind")), ParseEnd(Field("EndReason")),
            ParseCapture(Field("CaptureSource")), ParsePlatform(Field("SourcePlatform")), Empty(Field("SourceDeviceId")),
            int.Parse(Field("Revision"), CultureInfo.InvariantCulture), ParseTimestamp(Field("LastModifiedAtUtc")));
        if (WorklogEntryValidator.Validate(entry).Count != 0 ||
            entry.Duration.TotalSeconds != double.Parse(Field("DurationSeconds"), CultureInfo.InvariantCulture))
        {
            throw new FormatException("The record does not satisfy current worklog invariants.");
        }

        return entry;
    }

    private static DateTimeOffset ParseTimestamp(string value)
    {
        if (!DateTimeOffset.TryParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp))
        {
            throw new FormatException("Timestamp must use the round-trip offset-aware format.");
        }

        return timestamp;
    }
    private static string? Empty(string value) => value.Length == 0 ? null : value;
    private static ActivityKind ParseActivity(string value) => value == "active" ? ActivityKind.Active : throw new FormatException("Activity kind is invalid.");

    private static CaptureSource ParseCapture(string value) => value == "active-window" ? CaptureSource.ActiveWindow : throw new FormatException("Capture source is invalid.");

    private static ProjectAssignmentSource ParseProject(string value) => value switch { "unassigned" => ProjectAssignmentSource.Unassigned, "session" => ProjectAssignmentSource.Session, "rule" => ProjectAssignmentSource.Rule, "editor" => ProjectAssignmentSource.Editor, "imported" => ProjectAssignmentSource.Imported, _ => throw new FormatException("Project assignment source is invalid.") };

    private static EndReason ParseEnd(string value) => value switch { "application-change" => EndReason.ApplicationChange, "manual-pause" => EndReason.ManualPause, "idle-pause" => EndReason.IdlePause, "day-boundary" => EndReason.DayBoundary, "application-exit" => EndReason.ApplicationExit, "unknown" => EndReason.Unknown, _ => throw new FormatException("End reason is invalid.") };

    private static SourcePlatform ParsePlatform(string value) => value switch { "windows" => SourcePlatform.Windows, "linux" => SourcePlatform.Linux, "macos" => SourcePlatform.MacOS, "unknown" => SourcePlatform.Unknown, _ => throw new FormatException("Source platform is invalid.") };
}

/// <summary>Contains decoded records and diagnostics from a CSV read.</summary>
public sealed record CsvWorklogRead(WorklogOutcome Outcome, IReadOnlyList<TimeEntry> Entries)
{
    /// <summary>Creates an unsupported-schema result.</summary>
    /// <param name="message">The reason the schema is unsupported.</param>
    /// <returns>A typed unsupported-schema read result.</returns>
    public static CsvWorklogRead UnsupportedSchema(string message) => new(new WorklogOutcome(WorklogOutcomeKind.UnsupportedSchema, message), []);

    /// <summary>Creates an unsafe-malformed-data result.</summary>
    /// <param name="message">The reason record boundaries cannot be trusted.</param>
    /// <returns>A typed malformed-data read result.</returns>
    public static CsvWorklogRead Malformed(string message) => new(new WorklogOutcome(WorklogOutcomeKind.MalformedData, message), []);
}
