namespace FocusTimer.Persistence.Tests;

using FocusTimer.Core.Models;

public class CsvWorklogStoreTests
{
    [Fact]
    public async Task AppendAsync_GivenAdjacentDates_CreatesCurrentSchemaFilesInDailyPartitions()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = CreateStore(root);
            var first = CreateEntry("first", new DateTimeOffset(2026, 3, 31, 23, 0, 0, TimeSpan.Zero));
            var second = CreateEntry("second", new DateTimeOffset(2026, 4, 1, 9, 0, 0, TimeSpan.Zero));

            var outcome = await store.AppendAsync([first, second]);

            Assert.True(outcome.IsSuccess);
            var firstPath = Path.Combine(root, "2026", "03", "2026-03-31-worklog.csv");
            var secondPath = Path.Combine(root, "2026", "04", "2026-04-01-worklog.csv");
            Assert.True(File.Exists(firstPath));
            Assert.True(File.Exists(secondPath));
            Assert.StartsWith(string.Join(',', CsvWorklogCodec.Header), await File.ReadAllTextAsync(firstPath));
            Assert.StartsWith(string.Join(',', CsvWorklogCodec.Header), await File.ReadAllTextAsync(secondPath));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task AppendAsync_GivenInvalidMemberInMultiDayBatch_DoesNotCreateAnyFile()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = CreateStore(root);
            var valid = CreateEntry("valid", new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero));
            var invalid = CreateEntry("invalid", new DateTimeOffset(2026, 4, 1, 9, 0, 0, TimeSpan.Zero)) with { Revision = 0 };

            var outcome = await store.AppendAsync([valid, invalid]);

            Assert.Equal(WorklogOutcomeKind.ValidationFailure, outcome.Kind);
            Assert.Empty(Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task AppendAsync_GivenConflictingRetry_LeavesExistingRowUnchanged()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = CreateStore(root);
            var entry = CreateEntry("entry", new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero));
            Assert.True((await store.AppendAsync([entry])).IsSuccess);

            var outcome = await store.AppendAsync([entry with { AppName = "Changed" }]);
            var result = await store.QueryAsync(new WorklogQuery(entry.StartedAt, entry.EndedAt.AddMinutes(1)));

            Assert.Equal(WorklogOutcomeKind.Conflict, outcome.Kind);
            Assert.Equal(entry, Assert.Single(result.Entries));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task AppendAsync_GivenUnsupportedTargetDuringMultiDayPreflight_LeavesOtherTargetsUntouched()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = CreateStore(root);
            var first = CreateEntry("first", new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero));
            var second = CreateEntry("second", new DateTimeOffset(2026, 4, 1, 9, 0, 0, TimeSpan.Zero));
            var unsupportedPath = Path.Combine(root, "2026", "04", "2026-04-01-worklog.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(unsupportedPath)!);
            await File.WriteAllTextAsync(unsupportedPath, "Date,Application\r\n2026-04-01,Legacy\r\n");

            var outcome = await store.AppendAsync([first, second]);

            Assert.Equal(WorklogOutcomeKind.UnsupportedSchema, outcome.Kind);
            Assert.False(File.Exists(Path.Combine(root, "2026", "03", "2026-03-31-worklog.csv")));
            Assert.Equal("Date,Application\r\n2026-04-01,Legacy\r\n", await File.ReadAllTextAsync(unsupportedPath));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task QueryAsync_GivenMultiplePartitionsAndMissingDays_ReturnsChronologicalOverlapMatches()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = CreateStore(root);
            var first = CreateEntry("first", new DateTimeOffset(2026, 3, 30, 23, 0, 0, TimeSpan.Zero));
            var second = CreateEntry("second", new DateTimeOffset(2026, 4, 1, 9, 0, 0, TimeSpan.Zero));
            Assert.True((await store.AppendAsync([second, first])).IsSuccess);

            var result = await store.QueryAsync(new WorklogQuery(
                new DateTimeOffset(2026, 3, 30, 22, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 4, 2, 0, 0, 0, TimeSpan.Zero)));

            Assert.True(result.Outcome.IsSuccess);
            Assert.Empty(result.Outcome.Warnings ?? []);
            Assert.Equal([first, second], result.Entries);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task QueryAsync_GivenBoundaryOnlyEntries_ExcludesBothBoundaries()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = CreateStore(root);
            var before = CreateEntry("before", new DateTimeOffset(2026, 3, 31, 8, 55, 0, TimeSpan.Zero));
            var after = CreateEntry("after", new DateTimeOffset(2026, 3, 31, 9, 5, 0, TimeSpan.Zero));
            Assert.True((await store.AppendAsync([before, after])).IsSuccess);

            var result = await store.QueryAsync(new WorklogQuery(
                new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 3, 31, 9, 5, 0, TimeSpan.Zero)));

            Assert.True(result.Outcome.IsSuccess);
            Assert.Empty(result.Entries);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task QueryAsync_GivenCombinedFilters_ReturnsOnlyExactCaseInsensitiveMatch()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = CreateStore(root);
            var match = CreateEntry("match", new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero)) with
            {
                AppName = "Visual Studio",
                ProjectTag = "FocusTimer",
                SessionId = "session-a",
            };
            var differentProject = match with { EntryId = "project", ProjectTag = "Other" };
            var differentSession = match with { EntryId = "session", SessionId = "session-b" };
            Assert.True((await store.AppendAsync([match, differentProject, differentSession])).IsSuccess);

            var result = await store.QueryAsync(new WorklogQuery(
                match.StartedAt.AddMinutes(-1),
                match.EndedAt.AddMinutes(1),
                Application: "visual studio",
                Project: "focustimer",
                SessionId: "session-a",
                ActivityKind: ActivityKind.Active,
                CaptureSource: CaptureSource.ActiveWindow));

            Assert.True(result.Outcome.IsSuccess);
            Assert.Equal(match, Assert.Single(result.Entries));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task QueryAndGetAsync_GivenRecoverableMalformedRecord_ReturnEntriesWithWarning()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = CreateStore(root);
            var entry = CreateEntry("entry", new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero));
            Assert.True((await store.AppendAsync([entry])).IsSuccess);
            var path = Path.Combine(root, "2026", "03", "2026-03-31-worklog.csv");
            await File.AppendAllTextAsync(path, "1,too-few-fields\r\n");

            var query = await store.QueryAsync(new WorklogQuery(entry.StartedAt, entry.EndedAt.AddMinutes(1)));
            var lookup = await store.GetAsync(entry.EntryId);

            Assert.True(query.Outcome.IsSuccess);
            Assert.Equal(entry, Assert.Single(query.Entries));
            Assert.Single(query.Outcome.Warnings!);
            Assert.True(lookup.Outcome.IsSuccess);
            Assert.Equal(entry, Assert.Single(lookup.Entries));
            Assert.Single(lookup.Outcome.Warnings!);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task QueryAsync_GivenUnsupportedOrUntrustedFile_ReturnsTypedFailureInsteadOfEmptySuccess()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = CreateStore(root);
            var path = Path.Combine(root, "2026", "03", "2026-03-31-worklog.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, "Date,Application\r\n2026-03-31,Legacy\r\n");

            var unsupported = await store.QueryAsync(new WorklogQuery(
                new DateTimeOffset(2026, 3, 31, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero)));
            await File.WriteAllTextAsync(path, $"{string.Join(',', CsvWorklogCodec.Header)}\r\n1,\"unterminated");
            var malformed = await store.QueryAsync(new WorklogQuery(
                new DateTimeOffset(2026, 3, 31, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero)));

            Assert.Equal(WorklogOutcomeKind.UnsupportedSchema, unsupported.Outcome.Kind);
            Assert.Empty(unsupported.Entries);
            Assert.Equal(WorklogOutcomeKind.MalformedData, malformed.Outcome.Kind);
            Assert.Empty(malformed.Entries);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task QueryAsync_GivenFileInUse_ReturnsTypedFailureInsteadOfEmptySuccess()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = CreateStore(root);
            var entry = CreateEntry("entry", new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero));
            Assert.True((await store.AppendAsync([entry])).IsSuccess);
            var path = Path.Combine(root, "2026", "03", "2026-03-31-worklog.csv");
            using var lease = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            var result = await store.QueryAsync(new WorklogQuery(entry.StartedAt, entry.EndedAt.AddMinutes(1)));

            Assert.Equal(WorklogOutcomeKind.FileInUse, result.Outcome.Kind);
            Assert.Empty(result.Entries);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task AppendAsync_GivenUnwritableWorklogRoot_ReturnsIoFailure()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var invalidRoot = Path.Combine(root, "worklog-file");
            await File.WriteAllTextAsync(invalidRoot, "not a directory");
            var store = CreateStore(invalidRoot);
            var entry = CreateEntry("entry", new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero));

            var outcome = await store.AppendAsync([entry]);

            Assert.Equal(WorklogOutcomeKind.IoFailure, outcome.Kind);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task GetAsync_GivenWorklogRootThatIsAFile_ReturnsIoFailureInsteadOfEmptySuccess()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var invalidRoot = Path.Combine(root, "worklog-file");
            await File.WriteAllTextAsync(invalidRoot, "not a directory");

            var result = await CreateStore(invalidRoot).GetAsync("entry");
            var query = await CreateStore(invalidRoot).QueryAsync(new WorklogQuery(
                new DateTimeOffset(2026, 3, 31, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero)));

            Assert.Equal(WorklogOutcomeKind.IoFailure, result.Outcome.Kind);
            Assert.Empty(result.Entries);
            Assert.Equal(WorklogOutcomeKind.IoFailure, query.Outcome.Kind);
            Assert.Empty(query.Entries);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task StoreMethods_GivenCancelledToken_PropagateCancellation()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = CreateStore(root);
            using var source = new CancellationTokenSource();
            source.Cancel();
            var entry = CreateEntry("entry", new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero));

            await Assert.ThrowsAsync<OperationCanceledException>(() => store.AppendAsync([entry], source.Token));
            await Assert.ThrowsAsync<OperationCanceledException>(() => store.GetAsync(entry.EntryId, source.Token));
            await Assert.ThrowsAsync<OperationCanceledException>(() => store.QueryAsync(
                new WorklogQuery(entry.StartedAt, entry.EndedAt), source.Token));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task AppendAndQuery_GivenQuotedCurrentEntry_RoundTripsAndIsIdempotent()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var settings = new Settings { WorklogDirectory = root };
            var store = new CsvSessionRepository(new StubSettingsProvider(settings), NullLogger.Instance);
            var entry = new TimeEntry("entry", "session", new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 31, 9, 5, 0, TimeSpan.Zero), "Code, Editor", "A \"quoted\"\nwindow", null, ProjectAssignmentSource.Unassigned, null, ActivityKind.Active, EndReason.ManualPause, CaptureSource.ActiveWindow, SourcePlatform.Windows, "device", 1, DateTimeOffset.UtcNow);

            Assert.True((await store.AppendAsync([entry])).IsSuccess);
            Assert.True((await store.AppendAsync([entry])).IsSuccess);
            var result = await store.QueryAsync(new WorklogQuery(entry.StartedAt, entry.EndedAt.AddMinutes(1)));

            Assert.True(result.Outcome.IsSuccess);
            Assert.Single(result.Entries);
            Assert.Equal(entry, result.Entries[0]);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task GetAsync_GivenMissingEntryAndNoWorklogs_ReturnsEmptySuccess()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = new CsvSessionRepository(new StubSettingsProvider(new Settings { WorklogDirectory = root }), NullLogger.Instance);

            var result = await store.GetAsync("missing");

            Assert.True(result.Outcome.IsSuccess);
            Assert.Empty(result.Entries);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task QueryAsync_GivenExclusiveEndAtMidnight_DoesNotReadTheFollowingDay()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = new CsvSessionRepository(new StubSettingsProvider(new Settings { WorklogDirectory = root }), NullLogger.Instance);
            var start = new DateTimeOffset(2026, 3, 31, 23, 0, 0, TimeSpan.Zero);
            var entry = new TimeEntry("entry", "session", start, start.AddMinutes(30), "Code", "Window", null, ProjectAssignmentSource.Unassigned, null, ActivityKind.Active, EndReason.ManualPause, CaptureSource.ActiveWindow, SourcePlatform.Windows, null, 1, DateTimeOffset.UtcNow);
            Assert.True((await store.AppendAsync([entry])).IsSuccess);

            var dayStart = new DateTimeOffset(start.Date, start.Offset);
            var result = await store.QueryAsync(new WorklogQuery(dayStart, dayStart.AddDays(1)));

            Assert.True(result.Outcome.IsSuccess);
            Assert.Single(result.Entries);
        }
        finally { Directory.Delete(root, true); }
    }

    private sealed class StubSettingsProvider : FocusTimer.Core.Interfaces.ISettingsProvider
    {
        private readonly Settings _settings;
        public StubSettingsProvider(Settings settings) => this._settings = settings;
        public Task<Settings> LoadAsync() => Task.FromResult(this._settings);
        public Task SaveAsync(Settings settings) => Task.CompletedTask;
    }

    private static CsvSessionRepository CreateStore(string root) =>
        new(new StubSettingsProvider(new Settings { WorklogDirectory = root }), NullLogger.Instance);

    private static TimeEntry CreateEntry(string entryId, DateTimeOffset startedAt) => new(
        entryId,
        "session",
        startedAt,
        startedAt.AddMinutes(5),
        "Code",
        "Window",
        null,
        ProjectAssignmentSource.Unassigned,
        null,
        ActivityKind.Active,
        EndReason.ManualPause,
        CaptureSource.ActiveWindow,
        SourcePlatform.Windows,
        "device",
        1,
        DateTimeOffset.UtcNow);
}
