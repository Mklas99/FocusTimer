#pragma warning disable

namespace FocusTimer.Persistence.Tests;

using System.Globalization;
using System.Text;
using FocusTimer.Core.Models;

public class CsvWorklogCodecTests
{
    [Fact]
    public async Task ReadAsync_GivenReorderedHeader_RoundTripsEveryField()
    {
        var entry = CreateEntry();
        var header = CsvWorklogCodec.Header.Reverse().ToArray();
        var values = Values(entry).Reverse();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes($"{string.Join(',', header)}\r\n{string.Join(',', values)}\r\n"));

        var result = await new CsvWorklogCodec().ReadAsync(stream, "reordered.csv");

        Assert.True(result.Outcome.IsSuccess);
        Assert.Equal(entry, Assert.Single(result.Entries));
    }

    [Fact]
    public async Task WriteAndReadAsync_GivenCsvControlCharacters_PreservesTextAndOptionalValues()
    {
        var entry = CreateEntry() with
        {
            AppName = "Žluťoučký, editor",
            WindowTitle = "A \"quoted\"\r\nwindow",
            ProjectTag = null,
            ProjectRuleId = null,
            SourceDeviceId = null,
        };
        await using var stream = new MemoryStream();
        var codec = new CsvWorklogCodec();
        await codec.WriteAsync(stream, [entry]);
        stream.Position = 0;

        var result = await codec.ReadAsync(stream, "quoted.csv");

        Assert.True(result.Outcome.IsSuccess);
        Assert.Equal(entry, Assert.Single(result.Entries));
    }

    [Theory]
    [InlineData(ProjectAssignmentSource.Unassigned, EndReason.ApplicationChange, SourcePlatform.Windows)]
    [InlineData(ProjectAssignmentSource.Session, EndReason.ManualPause, SourcePlatform.Linux)]
    [InlineData(ProjectAssignmentSource.Rule, EndReason.IdlePause, SourcePlatform.MacOS)]
    [InlineData(ProjectAssignmentSource.Editor, EndReason.DayBoundary, SourcePlatform.Unknown)]
    [InlineData(ProjectAssignmentSource.Imported, EndReason.ApplicationExit, SourcePlatform.Windows)]
    [InlineData(ProjectAssignmentSource.Unassigned, EndReason.Unknown, SourcePlatform.Windows)]
    public async Task WriteAndReadAsync_GivenEveryControlledValue_RoundTripsStableSpellings(
        ProjectAssignmentSource projectAssignmentSource,
        EndReason endReason,
        SourcePlatform sourcePlatform)
    {
        var entry = CreateEntry() with
        {
            ProjectAssignmentSource = projectAssignmentSource,
            EndReason = endReason,
            SourcePlatform = sourcePlatform,
        };
        await using var stream = new MemoryStream();
        var codec = new CsvWorklogCodec();
        await codec.WriteAsync(stream, [entry]);
        stream.Position = 0;

        var result = await codec.ReadAsync(stream, "controlled-values.csv");

        Assert.True(result.Outcome.IsSuccess);
        Assert.Equal(entry, Assert.Single(result.Entries));
    }

    [Fact]
    public async Task ReadAsync_GivenRecoverableMalformedRecord_ReturnsEntriesAndWarning()
    {
        var entry = CreateEntry();
        var record = string.Join(',', Values(entry));
        var content = $"{string.Join(',', CsvWorklogCodec.Header)}\r\n{record}\r\n1,too-few-fields\r\n{record}\r\n";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var result = await new CsvWorklogCodec().ReadAsync(stream, "malformed-row.csv");

        Assert.True(result.Outcome.IsSuccess);
        Assert.Equal(2, result.Entries.Count);
        Assert.Single(result.Outcome.Warnings!);
        Assert.Equal(3, result.Outcome.Warnings![0].RecordNumber);
    }

    [Fact]
    public async Task ReadAsync_GivenUntrustedRecordBoundary_ReturnsMalformedData()
    {
        var content = $"{string.Join(',', CsvWorklogCodec.Header)}\r\n1,\"unterminated";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var result = await new CsvWorklogCodec().ReadAsync(stream, "broken.csv");

        Assert.Equal(WorklogOutcomeKind.MalformedData, result.Outcome.Kind);
        Assert.Empty(result.Entries);
    }

    [Fact]
    public async Task ReadAsync_GivenTimestampWithoutOffset_ReturnsARecordWarning()
    {
        var entry = CreateEntry();
        var values = Values(entry).ToArray();
        values[3] = "2026-03-31T09:00:00";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(
            $"{string.Join(',', CsvWorklogCodec.Header)}\r\n{string.Join(',', values)}\r\n"));

        var result = await new CsvWorklogCodec().ReadAsync(stream, "no-offset.csv");

        Assert.True(result.Outcome.IsSuccess);
        Assert.Empty(result.Entries);
        Assert.Single(result.Outcome.Warnings!);
    }

    [Fact]
    public async Task ReadAsync_GivenDuplicateHeader_ReturnsUnsupportedSchema()
    {
        var header = CsvWorklogCodec.Header.ToArray();
        header[1] = header[0];
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes($"{string.Join(',', header)}\r\n"));

        var result = await new CsvWorklogCodec().ReadAsync(stream, "duplicate-header.csv");

        Assert.Equal(WorklogOutcomeKind.UnsupportedSchema, result.Outcome.Kind);
        Assert.Empty(result.Entries);
    }

    [Fact]
    public async Task ReadAsync_GivenInvalidUtf8_ReturnsMalformedData()
    {
        await using var stream = new MemoryStream([0xc3, 0x28]);

        var result = await new CsvWorklogCodec().ReadAsync(stream, "invalid-utf8.csv");

        Assert.Equal(WorklogOutcomeKind.MalformedData, result.Outcome.Kind);
        Assert.Empty(result.Entries);
    }

    [Fact]
    public async Task WriteAsync_GivenInvalidEntry_DoesNotModifyTheStream()
    {
        var original = Encoding.UTF8.GetBytes("unchanged");
        await using var stream = new MemoryStream();
        await stream.WriteAsync(original);
        var invalid = CreateEntry() with { Revision = 0 };

        await Assert.ThrowsAsync<ArgumentException>(() => new CsvWorklogCodec().WriteAsync(stream, [invalid]));

        Assert.Equal(original, stream.ToArray());
    }

    [Fact]
    public async Task AppendAsync_GivenDevelopmentSchema_LeavesFileByteForByteUnchanged()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var entry = CreateEntry();
            var path = Path.Combine(root, "2026", "03", "2026-03-31-worklog.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var original = "Date,Application,Duration\r\n2026-03-31,Legacy,60\r\n";
            await File.WriteAllTextAsync(path, original, Encoding.UTF8);
            var store = new CsvSessionRepository(new StubSettingsProvider(new Settings { WorklogDirectory = root }));

            var outcome = await store.AppendAsync([entry]);

            Assert.Equal(WorklogOutcomeKind.UnsupportedSchema, outcome.Kind);
            Assert.Equal(original, await File.ReadAllTextAsync(path, Encoding.UTF8));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static TimeEntry CreateEntry() => new(
        "entry", "session", new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 3, 31, 9, 5, 0, TimeSpan.Zero), "Editor", "Window", "Project",
        ProjectAssignmentSource.Session, "rule", ActivityKind.Active, EndReason.ManualPause, CaptureSource.ActiveWindow,
        SourcePlatform.Windows, "device", 1, new DateTimeOffset(2026, 3, 31, 9, 5, 0, TimeSpan.Zero));

    private static IEnumerable<string> Values(TimeEntry entry) =>
    [
        CsvWorklogCodec.CurrentSchemaVersion, entry.EntryId, entry.SessionId,
        entry.StartedAt.ToString("O", CultureInfo.InvariantCulture), entry.EndedAt.ToString("O", CultureInfo.InvariantCulture),
        entry.Duration.TotalSeconds.ToString(CultureInfo.InvariantCulture), entry.AppName, entry.WindowTitle,
        entry.ProjectTag!, WorklogValueCodec.ToStoredValue(entry.ProjectAssignmentSource), entry.ProjectRuleId!,
        WorklogValueCodec.ToStoredValue(entry.ActivityKind), WorklogValueCodec.ToStoredValue(entry.EndReason),
        WorklogValueCodec.ToStoredValue(entry.CaptureSource), WorklogValueCodec.ToStoredValue(entry.SourcePlatform),
        entry.SourceDeviceId!, entry.Revision.ToString(CultureInfo.InvariantCulture),
        entry.LastModifiedAtUtc.ToString("O", CultureInfo.InvariantCulture),
    ];

    private sealed class StubSettingsProvider : FocusTimer.Core.Interfaces.ISettingsProvider
    {
        private readonly Settings _settings;
        public StubSettingsProvider(Settings settings) => this._settings = settings;
        public Task<Settings> LoadAsync() => Task.FromResult(this._settings);
        public Task SaveAsync(Settings settings) => Task.CompletedTask;
    }
}
