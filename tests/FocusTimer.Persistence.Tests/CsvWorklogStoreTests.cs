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

    [Fact]
    public async Task PatchAsync_GivenMatchingRevision_RewritesOnlyTargetAndIncrementsRevision()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = CreateStore(root);
            var entry = CreateEntry("patch", new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero));
            var unaffected = CreateEntry("other", entry.StartedAt.AddMinutes(10));
            Assert.True((await store.AppendAsync([entry, unaffected])).IsSuccess);
            var patch = new WorklogPatch(entry.StartedAt.AddMinutes(1), entry.EndedAt.AddMinutes(3), "Changed", "New title",
                "Project", ProjectAssignmentSource.Session, null, ActivityKind.Active, EndReason.ManualPause, CaptureSource.ActiveWindow);

            var outcome = await store.PatchAsync(entry.EntryId, entry.Revision, patch);
            var result = await store.QueryAsync(new WorklogQuery(entry.StartedAt, unaffected.EndedAt.AddMinutes(1)));

            Assert.True(outcome.IsSuccess);
            Assert.Equal(2, result.Entries.Count);
            var changed = Assert.Single(result.Entries, item => item.EntryId == entry.EntryId);
            Assert.Equal(2, changed.Revision);
            Assert.Equal(TimeSpan.FromMinutes(7), changed.Duration);
            Assert.Equal("Changed", changed.AppName);
            Assert.Equal(unaffected, Assert.Single(result.Entries, item => item.EntryId == unaffected.EntryId));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task DeleteAsync_GivenStaleRevision_LeavesOriginalBytesUnchanged()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = CreateStore(root);
            var entry = CreateEntry("delete", new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero));
            Assert.True((await store.AppendAsync([entry])).IsSuccess);
            var path = Path.Combine(root, "2026", "03", "2026-03-31-worklog.csv");
            var before = await File.ReadAllBytesAsync(path);

            var outcome = await store.DeleteAsync(entry.EntryId, entry.Revision + 1);

            Assert.Equal(WorklogOutcomeKind.Conflict, outcome.Kind);
            Assert.Equal(before, await File.ReadAllBytesAsync(path));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task PatchAsync_GivenDateMove_RejectsWithoutChangingTheDailyWorklog()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = CreateStore(root);
            var entry = CreateEntry("date-move", new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero));
            Assert.True((await store.AppendAsync([entry])).IsSuccess);
            var path = Path.Combine(root, "2026", "03", "2026-03-31-worklog.csv");
            var before = await File.ReadAllBytesAsync(path);
            var patch = new WorklogPatch(entry.StartedAt.AddDays(1), entry.EndedAt.AddDays(1), entry.AppName, entry.WindowTitle,
                entry.ProjectTag, entry.ProjectAssignmentSource, entry.ProjectRuleId, entry.ActivityKind, entry.EndReason, entry.CaptureSource);

            var outcome = await store.PatchAsync(entry.EntryId, entry.Revision, patch);

            Assert.Equal(WorklogOutcomeKind.ValidationFailure, outcome.Kind);
            Assert.Equal(before, await File.ReadAllBytesAsync(path));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task PatchAsync_GivenEndDateMove_RejectsWithoutChangingTheDailyWorklog()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = CreateStore(root);
            var entry = CreateEntry("end-date-move", new DateTimeOffset(2026, 3, 31, 23, 50, 0, TimeSpan.Zero));
            Assert.True((await store.AppendAsync([entry])).IsSuccess);
            var path = Path.Combine(root, "2026", "03", "2026-03-31-worklog.csv");
            var before = await File.ReadAllBytesAsync(path);
            var patch = new WorklogPatch(entry.StartedAt, entry.EndedAt.AddDays(1), entry.AppName, entry.WindowTitle,
                entry.ProjectTag, entry.ProjectAssignmentSource, entry.ProjectRuleId, entry.ActivityKind, entry.EndReason, entry.CaptureSource);

            var outcome = await store.PatchAsync(entry.EntryId, entry.Revision, patch);

            Assert.Equal(WorklogOutcomeKind.ValidationFailure, outcome.Kind);
            Assert.Equal(before, await File.ReadAllBytesAsync(path));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task DeleteAsync_GivenMatchingRevision_RemovesOnlyTheRequestedEntry()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = CreateStore(root);
            var deleted = CreateEntry("deleted", new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero));
            var retained = CreateEntry("retained", deleted.StartedAt.AddMinutes(10));
            Assert.True((await store.AppendAsync([deleted, retained])).IsSuccess);

            var outcome = await store.DeleteAsync(deleted.EntryId, deleted.Revision);
            var result = await store.QueryAsync(new WorklogQuery(deleted.StartedAt, retained.EndedAt.AddMinutes(1)));

            Assert.True(outcome.IsSuccess);
            Assert.Equal(retained, Assert.Single(result.Entries));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task MutationAsync_GivenMissingEntryOrInvalidRevision_ReturnsTypedOutcomeWithoutCreatingFiles()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = CreateStore(root);
            var patch = new WorklogPatch(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(1), "Code", "Window", null,
                ProjectAssignmentSource.Unassigned, null, ActivityKind.Active, EndReason.Unknown, CaptureSource.ActiveWindow);

            var notFound = await store.PatchAsync("missing", 1, patch);
            var invalid = await store.DeleteAsync("missing", 0);

            Assert.Equal(WorklogOutcomeKind.NotFound, notFound.Kind);
            Assert.Equal(WorklogOutcomeKind.ValidationFailure, invalid.Kind);
            Assert.Empty(Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task AppendAndPatchAsync_GivenConcurrentSameDayChanges_PreservesBothChanges()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = CreateStore(root);
            var existing = CreateEntry("existing", new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero));
            var appended = CreateEntry("appended", existing.StartedAt.AddMinutes(10));
            Assert.True((await store.AppendAsync([existing])).IsSuccess);
            var patch = new WorklogPatch(existing.StartedAt, existing.EndedAt, "Changed", existing.WindowTitle,
                existing.ProjectTag, existing.ProjectAssignmentSource, existing.ProjectRuleId, existing.ActivityKind,
                existing.EndReason, existing.CaptureSource);

            var outcomes = await Task.WhenAll(store.AppendAsync([appended]), store.PatchAsync(existing.EntryId, 1, patch));
            var result = await store.QueryAsync(new WorklogQuery(existing.StartedAt, appended.EndedAt.AddMinutes(1)));

            Assert.All(outcomes, outcome => Assert.True(outcome.IsSuccess));
            Assert.Equal(2, result.Entries.Count);
            Assert.Equal(2, Assert.Single(result.Entries, item => item.EntryId == existing.EntryId).Revision);
            Assert.Single(result.Entries, item => item.EntryId == appended.EntryId);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task PatchAsync_GivenActivationFailure_PreservesOriginalAndCleansTemporaryArtifact()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var operations = new FailingActivationOperations();
            var store = new CsvSessionRepository(new StubSettingsProvider(new Settings { WorklogDirectory = root }), NullLogger.Instance, operations);
            var entry = CreateEntry("activation", new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero));
            Assert.True((await store.AppendAsync([entry])).IsSuccess);
            var path = Path.Combine(root, "2026", "03", "2026-03-31-worklog.csv");
            var before = await File.ReadAllBytesAsync(path);
            operations.FailActivation = true;
            var patch = new WorklogPatch(entry.StartedAt, entry.EndedAt, "Changed", entry.WindowTitle, entry.ProjectTag,
                entry.ProjectAssignmentSource, entry.ProjectRuleId, entry.ActivityKind, entry.EndReason, entry.CaptureSource);

            var outcome = await store.PatchAsync(entry.EntryId, entry.Revision, patch);

            Assert.Equal(WorklogOutcomeKind.IoFailure, outcome.Kind);
            Assert.Equal(before, await File.ReadAllBytesAsync(path));
            Assert.Empty(Directory.EnumerateFiles(Path.GetDirectoryName(path)!, "*.worklog-rewrite-*.tmp"));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task PatchAsync_GivenTemporaryWriteFailure_PreservesOriginalAndCleansTemporaryArtifact()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var operations = new FailingActivationOperations();
            var store = new CsvSessionRepository(new StubSettingsProvider(new Settings { WorklogDirectory = root }), NullLogger.Instance, operations);
            var entry = CreateEntry("write-failure", new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero));
            Assert.True((await store.AppendAsync([entry])).IsSuccess);
            var path = Path.Combine(root, "2026", "03", "2026-03-31-worklog.csv");
            var before = await File.ReadAllBytesAsync(path);
            operations.FailTemporaryCreation = true;
            var patch = new WorklogPatch(entry.StartedAt, entry.EndedAt, "Changed", entry.WindowTitle, entry.ProjectTag,
                entry.ProjectAssignmentSource, entry.ProjectRuleId, entry.ActivityKind, entry.EndReason, entry.CaptureSource);

            var outcome = await store.PatchAsync(entry.EntryId, entry.Revision, patch);

            Assert.Equal(WorklogOutcomeKind.IoFailure, outcome.Kind);
            Assert.Equal(before, await File.ReadAllBytesAsync(path));
            Assert.Empty(Directory.EnumerateFiles(Path.GetDirectoryName(path)!, "*.worklog-rewrite-*.tmp"));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task DeleteAsync_CleansRecognizedStaleArtifactsWithoutTouchingOtherFiles()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = CreateStore(root);
            var entry = CreateEntry("cleanup", new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero));
            Assert.True((await store.AppendAsync([entry])).IsSuccess);
            var directory = Path.Combine(root, "2026", "03");
            var stale = Path.Combine(directory, "2026-03-31-worklog.csv.worklog-rewrite-interrupted.tmp");
            var unrelated = Path.Combine(directory, "notes.tmp");
            await File.WriteAllTextAsync(stale, "partial");
            File.SetLastWriteTimeUtc(stale, DateTime.UtcNow.AddMinutes(-6));
            await File.WriteAllTextAsync(unrelated, "keep");

            Assert.True((await store.DeleteAsync(entry.EntryId, entry.Revision)).IsSuccess);

            Assert.False(File.Exists(stale));
            Assert.True(File.Exists(unrelated));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void CleanupStaleTemporaryFiles_GivenRecentRecognizedArtifact_LeavesItUntouched()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var path = Path.Combine(root, "2026", "03", "2026-03-31-worklog.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var recent = path + ".worklog-rewrite-in-progress.tmp";
            File.WriteAllText(recent, "in progress");

            new AtomicWorklogFileOperations().CleanupStaleTemporaryFiles(path);

            Assert.True(File.Exists(recent));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task AppendAsync_AppliesRetentionOnlyToEligibleFinalizedCurrentSchemaFiles()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var settings = new Settings { WorklogDirectory = root, DataRetentionDays = 0 };
            var store = new CsvSessionRepository(new StubSettingsProvider(settings), NullLogger.Instance);
            var oldStart = DateTimeOffset.Now.Date.AddDays(-2);
            var expired = CreateEntry("expired", new DateTimeOffset(oldStart, DateTimeOffset.Now.Offset));
            Assert.True((await store.AppendAsync([expired])).IsSuccess);
            var expiredPath = Path.Combine(root, expired.StartedAt.ToString("yyyy"), expired.StartedAt.ToString("MM"),
                expired.StartedAt.ToString("yyyy-MM-dd") + "-worklog.csv");
            var temporaryPath = expiredPath + ".worklog-rewrite-interrupted.tmp";
            await File.WriteAllTextAsync(temporaryPath, "temporary");
            settings.DataRetentionDays = 1;

            var current = CreateEntry("current", new DateTimeOffset(DateTimeOffset.Now.Date, DateTimeOffset.Now.Offset));
            Assert.True((await store.AppendAsync([current])).IsSuccess);

            Assert.False(File.Exists(expiredPath));
            Assert.True(File.Exists(temporaryPath));
            Assert.True(File.Exists(Path.Combine(root, current.StartedAt.ToString("yyyy"), current.StartedAt.ToString("MM"),
                current.StartedAt.ToString("yyyy-MM-dd") + "-worklog.csv")));
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

    private sealed class FailingActivationOperations : IAtomicWorklogFileOperations
    {
        private readonly AtomicWorklogFileOperations _inner = new();

        public bool FailActivation { get; set; }

        public bool FailTemporaryCreation { get; set; }

        public FileStream CreateTemporaryFile(string targetPath, out string temporaryPath)
        {
            if (this.FailTemporaryCreation)
            {
                temporaryPath = string.Empty;
                throw new IOException("Injected temporary-file creation failure.");
            }

            return this._inner.CreateTemporaryFile(targetPath, out temporaryPath);
        }

        public void ActivateReplacement(string temporaryPath, string targetPath)
        {
            if (this.FailActivation)
            {
                throw new IOException("Injected activation failure.");
            }

            this._inner.ActivateReplacement(temporaryPath, targetPath);
        }

        public void DeleteTemporaryFile(string temporaryPath) => this._inner.DeleteTemporaryFile(temporaryPath);

        public void CleanupStaleTemporaryFiles(string targetPath) => this._inner.CleanupStaleTemporaryFiles(targetPath);
    }
}
