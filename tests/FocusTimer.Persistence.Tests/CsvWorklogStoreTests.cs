namespace FocusTimer.Persistence.Tests;

using FocusTimer.Core.Models;

public class CsvWorklogStoreTests
{
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
}
