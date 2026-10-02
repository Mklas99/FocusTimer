namespace FocusTimer.Persistence.Tests;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

public class ManualEntryStorageTests
{
    private static readonly DateTimeOffset Day = new(2026, 3, 31, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AppendAsync_GivenManualEntry_RoundTripsEveryField()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = CreateStore(root);
            var manual = Entry("manual", Day, CaptureSource.Manual) with
            {
                AppName = "Manual entry",
                WindowTitle = string.Empty,
                ProjectTag = "Alpha",
                ProjectAssignmentSource = ProjectAssignmentSource.Editor,
                EndReason = EndReason.Unknown,
            };

            Assert.True((await store.AppendAsync([manual])).IsSuccess);
            var read = await store.GetAsync("manual");

            Assert.True(read.Outcome.IsSuccess);
            var stored = Assert.Single(read.Entries);
            Assert.Equal(manual, stored);
            Assert.Equal(CaptureSource.Manual, stored.CaptureSource);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task QueryAsync_GivenManualCaptureFilter_ReturnsOnlyManualEntries()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = CreateStore(root);
            Assert.True((await store.AppendAsync([
                Entry("tracked", Day, CaptureSource.ActiveWindow),
                Entry("manual", Day.AddHours(1), CaptureSource.Manual)])).IsSuccess);

            var read = await store.QueryAsync(new WorklogQuery(new DateTimeOffset(2026, 3, 31, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero), CaptureSource: CaptureSource.Manual));

            Assert.Equal("manual", Assert.Single(read.Entries).EntryId);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Store_GivenFileWithOnlyActiveWindowEntries_AppendsPatchesAndDeletesWithoutSchemaChange()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = CreateStore(root);
            var first = Entry("one", Day, CaptureSource.ActiveWindow);
            Assert.True((await store.AppendAsync([first])).IsSuccess);
            var path = Path.Combine(root, "2026", "03", "2026-03-31-worklog.csv");
            var header = (await File.ReadAllLinesAsync(path))[0];

            Assert.True((await store.AppendAsync([Entry("two", Day.AddHours(1), CaptureSource.ActiveWindow)])).IsSuccess);
            Assert.True((await store.PatchAsync("one", 1, ToPatch(first with { WindowTitle = "Renamed" }))).IsSuccess);
            Assert.True((await store.DeleteAsync("two", 1)).IsSuccess);

            Assert.Equal(header, (await File.ReadAllLinesAsync(path))[0]);
            var read = await store.GetAsync("one");
            Assert.Equal("Renamed", Assert.Single(read.Entries).WindowTitle);
            Assert.Equal(CaptureSource.ActiveWindow, read.Entries[0].CaptureSource);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task PatchAsync_GivenOlderEntryEndingAtNextMidnight_AcceptsShorteningAndRenaming()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = CreateStore(root);
            var start = new DateTimeOffset(2026, 3, 31, 23, 30, 0, TimeSpan.Zero);
            var legacy = Entry("legacy", start, CaptureSource.ActiveWindow) with { EndedAt = start.AddMinutes(30) };
            Assert.True((await store.AppendAsync([legacy])).IsSuccess);

            var renamed = await store.PatchAsync("legacy", 1, ToPatch(legacy with { WindowTitle = "Renamed" }));
            var shortened = await store.PatchAsync("legacy", 2, ToPatch(legacy with { EndedAt = start.AddMinutes(20) }));

            Assert.True(renamed.IsSuccess, renamed.Message);
            Assert.True(shortened.IsSuccess, shortened.Message);
            var stored = Assert.Single((await store.GetAsync("legacy")).Entries);
            Assert.Equal(start.AddMinutes(20), stored.EndedAt);
            Assert.Equal(3, stored.Revision);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task PatchAsync_GivenEntryEndingBeforeMidnight_RejectsExtensionToNextMidnight()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = CreateStore(root);
            var start = new DateTimeOffset(2026, 3, 31, 23, 0, 0, TimeSpan.Zero);
            var entry = Entry("late", start, CaptureSource.ActiveWindow);
            Assert.True((await store.AppendAsync([entry])).IsSuccess);

            var outcome = await store.PatchAsync("late", 1, ToPatch(entry with { EndedAt = start.AddHours(1) }));

            Assert.Equal(WorklogOutcomeKind.ValidationFailure, outcome.Kind);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task PatchAsync_GivenStaleRevisionAfterEarlierEdit_ConflictsAndLeavesFileUnchanged()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = CreateStore(root);
            var entry = Entry("e", Day, CaptureSource.ActiveWindow);
            Assert.True((await store.AppendAsync([entry])).IsSuccess);
            Assert.True((await store.PatchAsync("e", 1, ToPatch(entry with { WindowTitle = "First" }))).IsSuccess);
            var path = Path.Combine(root, "2026", "03", "2026-03-31-worklog.csv");
            var before = await File.ReadAllBytesAsync(path);

            var stale = await store.PatchAsync("e", 1, ToPatch(entry with { WindowTitle = "Second" }));
            var staleDelete = await store.DeleteAsync("e", 1);

            Assert.Equal(WorklogOutcomeKind.Conflict, stale.Kind);
            Assert.Equal(WorklogOutcomeKind.Conflict, staleDelete.Kind);
            Assert.Equal(before, await File.ReadAllBytesAsync(path));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Store_GivenTrackerAppendsWhileUserEditsAndDeletes_KeepsEveryChangeAndAValidFile()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var store = CreateStore(root);
            var edited = Entry("edited", Day, CaptureSource.ActiveWindow);
            var deleted = Entry("deleted", Day.AddHours(1), CaptureSource.ActiveWindow);
            Assert.True((await store.AppendAsync([edited, deleted])).IsSuccess);

            var appends = Enumerable.Range(0, 20)
                .Select(i => store.AppendAsync([Entry("tracked-" + i, Day.AddHours(2).AddMinutes(i * 31), CaptureSource.ActiveWindow)]));
            var patch = store.PatchAsync("edited", 1, ToPatch(edited with { WindowTitle = "User edit" }));
            var delete = store.DeleteAsync("deleted", 1);

            var outcomes = await Task.WhenAll(appends.Append(patch).Append(delete));

            Assert.All(outcomes, outcome => Assert.True(outcome.IsSuccess || outcome.Kind == WorklogOutcomeKind.Conflict, outcome.Message));
            Assert.True(outcomes[^2].IsSuccess);
            Assert.True(outcomes[^1].IsSuccess);
            var read = await store.QueryAsync(new WorklogQuery(
                new DateTimeOffset(2026, 3, 31, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero)));
            Assert.True(read.Outcome.IsSuccess);
            Assert.Empty(read.Outcome.Warnings ?? []);
            Assert.Equal("User edit", read.Entries.Single(e => e.EntryId == "edited").WindowTitle);
            Assert.DoesNotContain(read.Entries, e => e.EntryId == "deleted");
            Assert.Equal(outcomes.Take(20).Count(o => o.IsSuccess), read.Entries.Count(e => e.EntryId.StartsWith("tracked-")));
        }
        finally { Directory.Delete(root, true); }
    }

    private static CsvSessionRepository CreateStore(string root) =>
        new(new StubSettingsProvider(new Settings { WorklogDirectory = root }), NullLogger.Instance);

    private static WorklogPatch ToPatch(TimeEntry e) => new(e.StartedAt, e.EndedAt, e.AppName, e.WindowTitle, e.ProjectTag,
        e.ProjectAssignmentSource, e.ProjectRuleId, e.ActivityKind, e.EndReason, e.CaptureSource);

    private static TimeEntry Entry(string id, DateTimeOffset start, CaptureSource source) => new(
        id, "session-" + id, start, start.AddMinutes(30), "Code", "Window", null, ProjectAssignmentSource.Unassigned, null,
        ActivityKind.Active, EndReason.ManualPause, source, SourcePlatform.Windows, "device", 1,
        new DateTimeOffset(2026, 3, 31, 12, 0, 0, TimeSpan.Zero));

    private sealed class StubSettingsProvider : ISettingsProvider
    {
        private readonly Settings _settings;
        public StubSettingsProvider(Settings settings) => this._settings = settings;
        public Task<Settings> LoadAsync() => Task.FromResult(this._settings);
        public Task SaveAsync(Settings settings) => Task.CompletedTask;
    }
}
