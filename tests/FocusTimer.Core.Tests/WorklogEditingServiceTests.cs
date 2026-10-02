namespace FocusTimer.Core.Tests;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

public class WorklogEditingServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 4, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Yesterday = new(2026, 5, 3);
    private static readonly DateOnly Today = new(2026, 5, 4);

    [Fact]
    public async Task AddManualAsync_GivenValidEntry_StoresFixedLabelManualSourceAndEveryField()
    {
        var h = new Harness();

        var result = await h.Service.AddManualAsync(new(Yesterday, new TimeOnly(14, 0), TimeSpan.FromMinutes(45), "Report.docx", "Alpha"));

        Assert.True(result.IsSuccess, result.Message);
        var stored = Assert.Single(h.Store.Entries);
        Assert.Equal("Manual entry", stored.AppName);
        Assert.Equal(CaptureSource.Manual, stored.CaptureSource);
        Assert.Equal(new DateTimeOffset(2026, 5, 3, 14, 0, 0, TimeSpan.Zero), stored.StartedAt);
        Assert.Equal(new DateTimeOffset(2026, 5, 3, 14, 45, 0, TimeSpan.Zero), stored.EndedAt);
        Assert.Equal("Report.docx", stored.WindowTitle);
        Assert.Equal("Alpha", stored.ProjectTag);
        Assert.Equal(ProjectAssignmentSource.Editor, stored.ProjectAssignmentSource);
        Assert.Equal(1, stored.Revision);
        Assert.Equal("device-1", stored.SourceDeviceId);
        Assert.NotEqual(stored.EntryId, stored.SessionId);
        Assert.Empty(WorklogEntryValidator.Validate(stored));
        Assert.Equal(stored, result.Entry);
    }

    [Fact]
    public async Task AddManualAsync_GivenNoProjectOrWindow_StoresUnassignedAndEmptyTitle()
    {
        var h = new Harness();

        await h.Service.AddManualAsync(new(Yesterday, new TimeOnly(9, 0), TimeSpan.FromHours(1), null, "  "));

        var stored = Assert.Single(h.Store.Entries);
        Assert.Equal(string.Empty, stored.WindowTitle);
        Assert.Null(stored.ProjectTag);
        Assert.Equal(ProjectAssignmentSource.Unassigned, stored.ProjectAssignmentSource);
    }

    [Fact]
    public async Task AddManualAsync_GivenSurroundingWhitespace_TrimsProjectAndWindow()
    {
        var h = new Harness();

        await h.Service.AddManualAsync(new(Yesterday, new TimeOnly(9, 0), TimeSpan.FromHours(1), "  Notes  ", "  Alpha  "));

        var stored = Assert.Single(h.Store.Entries);
        Assert.Equal("Alpha", stored.ProjectTag);
        Assert.Equal("Notes", stored.WindowTitle);
    }

    [Fact]
    public async Task AddManualAsync_GivenProjectOver100Characters_RejectsWithoutStoring()
    {
        var h = new Harness();

        var result = await h.Service.AddManualAsync(new(Yesterday, new TimeOnly(9, 0), TimeSpan.FromHours(1), null, new string('x', 101)));

        Assert.Equal(WorklogOutcomeKind.ValidationFailure, result.Kind);
        Assert.Contains("100", result.Message);
        Assert.Empty(h.Store.Entries);
        Assert.True((await h.Service.AddManualAsync(new(Yesterday, new TimeOnly(9, 0), TimeSpan.FromHours(1), null, new string('x', 100)))).IsSuccess);
    }

    [Theory]
    [InlineData(23, 0, 60, false)]
    [InlineData(23, 0, 59, true)]
    [InlineData(23, 30, 45, false)]
    public async Task AddManualAsync_GivenEntryNearEndOfDay_RejectsOnlyWhenPast2359(int hour, int minute, int minutes, bool accepted)
    {
        var h = new Harness();

        var result = await h.Service.AddManualAsync(new(Yesterday, new TimeOnly(hour, minute), TimeSpan.FromMinutes(minutes), null, null));

        Assert.Equal(accepted, result.IsSuccess);
        Assert.Equal(accepted ? 1 : 0, h.Store.Entries.Count);
        if (!accepted)
        {
            Assert.Contains("23:59", result.Message);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task AddManualAsync_GivenZeroOrNegativeDuration_RejectsWithoutStoring(int minutes)
    {
        var h = new Harness();

        var result = await h.Service.AddManualAsync(new(Yesterday, new TimeOnly(9, 0), TimeSpan.FromMinutes(minutes), null, null));

        Assert.Equal(WorklogOutcomeKind.ValidationFailure, result.Kind);
        Assert.Empty(h.Store.Entries);
    }

    [Fact]
    public async Task AddManualAsync_GivenEntryEndingInFuture_RejectsButAcceptsEndingExactlyNow()
    {
        var h = new Harness();

        var future = await h.Service.AddManualAsync(new(Today, new TimeOnly(9, 30), TimeSpan.FromHours(1), null, null));
        var exact = await h.Service.AddManualAsync(new(Today, new TimeOnly(9, 0), TimeSpan.FromHours(1), null, null));
        var tomorrow = await h.Service.AddManualAsync(new(Today.AddDays(1), new TimeOnly(0, 0), TimeSpan.FromHours(1), null, null));

        Assert.False(future.IsSuccess);
        Assert.Contains("future", future.Message);
        Assert.True(exact.IsSuccess, exact.Message);
        Assert.False(tomorrow.IsSuccess);
        Assert.Single(h.Store.Entries);
    }

    [Fact]
    public async Task AddManualAsync_GivenWorkLoggingDisabled_StillStoresTheEntry()
    {
        var h = new Harness(new Settings { WorkLoggingEnabled = false });

        var result = await h.Service.AddManualAsync(new(Yesterday, new TimeOnly(9, 0), TimeSpan.FromHours(1), null, null));

        Assert.True(result.IsSuccess, result.Message);
        Assert.Single(h.Store.Entries);
    }

    [Fact]
    public async Task AddManualAsync_GivenDayOlderThanRetention_RejectsAndNamesEarliestDay()
    {
        var h = new Harness(new Settings { DataRetentionDays = 3 });

        var tooOld = await h.Service.AddManualAsync(new(Today.AddDays(-3), new TimeOnly(9, 0), TimeSpan.FromHours(1), null, null));
        var oldest = await h.Service.AddManualAsync(new(Today.AddDays(-2), new TimeOnly(9, 0), TimeSpan.FromHours(1), null, null));

        Assert.Equal(WorklogOutcomeKind.ValidationFailure, tooOld.Kind);
        Assert.Contains("2026-05-02", tooOld.Message);
        Assert.True(oldest.IsSuccess, oldest.Message);
    }

    [Fact]
    public async Task AddManualAsync_GivenRetentionOff_AcceptsAnyPastDay()
    {
        var h = new Harness(new Settings { DataRetentionDays = 0 });

        var result = await h.Service.AddManualAsync(new(new DateOnly(2020, 1, 1), new TimeOnly(9, 0), TimeSpan.FromHours(1), null, null));

        Assert.True(result.IsSuccess, result.Message);
    }

    [Fact]
    public async Task AddManualAsync_GivenStartInSpringForwardGap_Rejects()
    {
        var h = new Harness(new Settings { DataRetentionDays = 0 }, Vienna(), new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero));

        var result = await h.Service.AddManualAsync(new(new DateOnly(2026, 3, 29), new TimeOnly(2, 30), TimeSpan.FromMinutes(30), null, null));

        Assert.Equal(WorklogOutcomeKind.ValidationFailure, result.Kind);
        Assert.Contains("clocks change", result.Message);
        Assert.Empty(h.Store.Entries);
    }

    [Fact]
    public async Task AddManualAsync_GivenAmbiguousStart_UsesEarlierOccurrence()
    {
        var h = new Harness(new Settings { DataRetentionDays = 0 }, Vienna(), new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero));

        var result = await h.Service.AddManualAsync(new(new DateOnly(2026, 10, 25), new TimeOnly(2, 30), TimeSpan.FromMinutes(30), null, null));

        Assert.True(result.IsSuccess, result.Message);
        Assert.Equal(TimeSpan.FromHours(2), Assert.Single(h.Store.Entries).StartedAt.Offset);
    }

    [Fact]
    public async Task AddManualAsync_GivenEntryAcrossSpringForward_UsesElapsedTime()
    {
        var h = new Harness(new Settings { DataRetentionDays = 0 }, Vienna(), new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero));

        var result = await h.Service.AddManualAsync(new(new DateOnly(2026, 3, 29), new TimeOnly(1, 0), TimeSpan.FromHours(2), null, null));

        Assert.True(result.IsSuccess, result.Message);
        var stored = Assert.Single(h.Store.Entries);
        Assert.Equal(TimeSpan.FromHours(2), stored.Duration);
        Assert.Equal(new TimeOnly(4, 0), TimeOnly.FromDateTime(stored.EndedAt.DateTime));
    }

    [Fact]
    public async Task AddManualAsync_GivenOverlapWithTrackedEntry_SavesAndReturnsOverlap()
    {
        var h = new Harness();
        var tracked = Tracked("t1", new DateTimeOffset(2026, 5, 3, 14, 30, 0, TimeSpan.Zero), 60);
        h.Store.Entries.Add(tracked);

        var result = await h.Service.AddManualAsync(new(Yesterday, new TimeOnly(14, 0), TimeSpan.FromHours(1), null, null));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, h.Store.Entries.Count);
        Assert.Equal(tracked.EntryId, Assert.Single(result.OverlappingEntries).EntryId);
    }

    [Fact]
    public async Task AddManualAsync_GivenEntryOnlyTouchingAnother_ReportsNoOverlap()
    {
        var h = new Harness();
        h.Store.Entries.Add(Tracked("t1", new DateTimeOffset(2026, 5, 3, 15, 0, 0, TimeSpan.Zero), 30));

        var result = await h.Service.AddManualAsync(new(Yesterday, new TimeOnly(14, 0), TimeSpan.FromHours(1), null, null));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.OverlappingEntries);
    }

    [Fact]
    public void FindOverlaps_ExcludesSubjectAndSortsByStart()
    {
        var subject = Tracked("s", new DateTimeOffset(2026, 5, 3, 10, 0, 0, TimeSpan.Zero), 60);
        var late = Tracked("b", new DateTimeOffset(2026, 5, 3, 10, 30, 0, TimeSpan.Zero), 60);
        var early = Tracked("a", new DateTimeOffset(2026, 5, 3, 9, 30, 0, TimeSpan.Zero), 45);
        var before = Tracked("c", new DateTimeOffset(2026, 5, 3, 9, 0, 0, TimeSpan.Zero), 60);

        var overlaps = WorklogEditingService.FindOverlaps([subject, late, early, before], subject);

        Assert.Equal(["a", "b"], overlaps.Select(e => e.EntryId));
    }

    [Fact]
    public async Task AddManualAsync_WhenStoreRejects_ReturnsFailureAndPublishesNothing()
    {
        var h = new Harness();
        h.Store.NextOutcome = new WorklogOutcome(WorklogOutcomeKind.UnsupportedSchema, "x");

        var result = await h.Service.AddManualAsync(new(Yesterday, new TimeOnly(9, 0), TimeSpan.FromHours(1), null, null));

        Assert.Equal(WorklogOutcomeKind.UnsupportedSchema, result.Kind);
        Assert.Contains("format", result.Message);
        Assert.Empty(h.Events);
    }

    [Fact]
    public async Task UpdateAsync_GivenShorterDuration_KeepsStartMovesEndAndIncrementsRevision()
    {
        var h = new Harness();
        var entry = Tracked("e", new DateTimeOffset(2026, 5, 3, 9, 0, 0, TimeSpan.Zero), 30);
        h.Store.Entries.Add(entry);

        var result = await h.Service.UpdateAsync(entry, new(entry.WindowTitle, null, TimeSpan.FromMinutes(20)));

        Assert.True(result.IsSuccess, result.Message);
        var stored = Assert.Single(h.Store.Entries);
        Assert.Equal(entry.StartedAt, stored.StartedAt);
        Assert.Equal(entry.StartedAt.AddMinutes(20), stored.EndedAt);
        Assert.Equal(2, stored.Revision);
        Assert.Equal(entry.AppName, stored.AppName);
        Assert.Equal(CaptureSource.ActiveWindow, stored.CaptureSource);
    }

    [Fact]
    public async Task UpdateAsync_GivenLongerDuration_MovesEndLater()
    {
        var h = new Harness();
        var entry = Tracked("e", new DateTimeOffset(2026, 5, 3, 9, 0, 0, TimeSpan.Zero), 30);
        h.Store.Entries.Add(entry);

        var result = await h.Service.UpdateAsync(entry, new(entry.WindowTitle, null, TimeSpan.FromHours(2)));

        Assert.True(result.IsSuccess);
        Assert.Equal(entry.StartedAt.AddHours(2), Assert.Single(h.Store.Entries).EndedAt);
    }

    [Fact]
    public async Task UpdateAsync_GivenTitleOnlyEditOfEntryWithSeconds_KeepsExactEndAndNeverRounds()
    {
        var h = new Harness();
        var start = new DateTimeOffset(2026, 5, 3, 9, 0, 0, TimeSpan.Zero);
        var entry = Tracked("e", start, 30) with { EndedAt = start.AddMinutes(30).AddSeconds(12) };
        h.Store.Entries.Add(entry);

        var result = await h.Service.UpdateAsync(entry, new("New title", null, DurationParser.TryParse("30m", out var d, out _) ? d : default));

        Assert.True(result.IsSuccess, result.Message);
        var stored = Assert.Single(h.Store.Entries);
        Assert.Equal(entry.EndedAt, stored.EndedAt);
        Assert.Equal("New title", stored.WindowTitle);
    }

    [Fact]
    public async Task UpdateAsync_GivenProjectSet_UsesEditorSourceAndClearsRuleId()
    {
        var h = new Harness();
        var entry = Tracked("e", new DateTimeOffset(2026, 5, 3, 9, 0, 0, TimeSpan.Zero), 30);
        h.Store.Entries.Add(entry with { ProjectTag = "Old", ProjectAssignmentSource = ProjectAssignmentSource.Rule, ProjectRuleId = "rule-1" });
        var loaded = h.Store.Entries[0];

        var result = await h.Service.UpdateAsync(loaded, new(loaded.WindowTitle, " New ", loaded.Duration));

        Assert.True(result.IsSuccess);
        var stored = Assert.Single(h.Store.Entries);
        Assert.Equal("New", stored.ProjectTag);
        Assert.Equal(ProjectAssignmentSource.Editor, stored.ProjectAssignmentSource);
        Assert.Null(stored.ProjectRuleId);
    }

    [Fact]
    public async Task UpdateAsync_GivenProjectCleared_BecomesUnassigned()
    {
        var h = new Harness();
        var entry = Tracked("e", new DateTimeOffset(2026, 5, 3, 9, 0, 0, TimeSpan.Zero), 30) with
        {
            ProjectTag = "Old",
            ProjectAssignmentSource = ProjectAssignmentSource.Session,
        };
        h.Store.Entries.Add(entry);

        var result = await h.Service.UpdateAsync(entry, new(entry.WindowTitle, "", entry.Duration));

        Assert.True(result.IsSuccess);
        var stored = Assert.Single(h.Store.Entries);
        Assert.Null(stored.ProjectTag);
        Assert.Equal(ProjectAssignmentSource.Unassigned, stored.ProjectAssignmentSource);
    }

    [Fact]
    public async Task UpdateAsync_GivenProjectUnchanged_PreservesProjectSourceAndRuleId()
    {
        var h = new Harness();
        var entry = Tracked("e", new DateTimeOffset(2026, 5, 3, 9, 0, 0, TimeSpan.Zero), 30) with
        {
            ProjectTag = "Old",
            ProjectAssignmentSource = ProjectAssignmentSource.Rule,
            ProjectRuleId = "rule-1",
        };
        h.Store.Entries.Add(entry);

        var result = await h.Service.UpdateAsync(entry, new("Renamed", "Old", TimeSpan.FromMinutes(20)));

        Assert.True(result.IsSuccess);
        var stored = Assert.Single(h.Store.Entries);
        Assert.Equal(ProjectAssignmentSource.Rule, stored.ProjectAssignmentSource);
        Assert.Equal("rule-1", stored.ProjectRuleId);
    }

    [Fact]
    public async Task UpdateAsync_GivenDurationPastDayLimit_RejectsAndLeavesEntryUnchanged()
    {
        var h = new Harness();
        var entry = Tracked("late", new DateTimeOffset(2026, 5, 3, 23, 0, 0, TimeSpan.Zero), 30);
        h.Store.Entries.Add(entry);

        var result = await h.Service.UpdateAsync(entry, new(entry.WindowTitle, null, TimeSpan.FromHours(1)));

        Assert.Equal(WorklogOutcomeKind.ValidationFailure, result.Kind);
        Assert.Equal(entry, Assert.Single(h.Store.Entries));
    }

    [Fact]
    public async Task UpdateAsync_GivenNothingChanged_SucceedsWithoutWriting()
    {
        var h = new Harness();
        var entry = Tracked("e", new DateTimeOffset(2026, 5, 3, 9, 0, 0, TimeSpan.Zero), 30);
        h.Store.Entries.Add(entry);

        var result = await h.Service.UpdateAsync(entry, new(entry.WindowTitle, null, entry.Duration));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, h.Store.PatchCalls);
        Assert.Empty(h.Events);
    }

    [Fact]
    public async Task UpdateAsync_GivenStaleRevision_ReportsConflictAndKeepsStoredEntry()
    {
        var h = new Harness();
        var entry = Tracked("e", new DateTimeOffset(2026, 5, 3, 9, 0, 0, TimeSpan.Zero), 30);
        h.Store.Entries.Add(entry with { Revision = 2 });

        var result = await h.Service.UpdateAsync(entry, new("Changed", null, entry.Duration));

        Assert.Equal(WorklogOutcomeKind.Conflict, result.Kind);
        Assert.True(result.NeedsReload);
        Assert.Contains("changed elsewhere", result.Message);
        Assert.Equal(entry.WindowTitle, Assert.Single(h.Store.Entries).WindowTitle);
    }

    [Fact]
    public async Task UpdateAsync_GivenMissingEntry_ReportsNotFound()
    {
        var h = new Harness();
        var entry = Tracked("gone", new DateTimeOffset(2026, 5, 3, 9, 0, 0, TimeSpan.Zero), 30);

        var result = await h.Service.UpdateAsync(entry, new("Changed", null, entry.Duration));

        Assert.Equal(WorklogOutcomeKind.NotFound, result.Kind);
        Assert.True(result.NeedsReload);
        Assert.Contains("no longer exists", result.Message);
    }

    [Fact]
    public async Task UpdateAsync_GivenLengthenedEntryOverlappingNeighbour_ReturnsOverlap()
    {
        var h = new Harness();
        var entry = Tracked("a", new DateTimeOffset(2026, 5, 3, 9, 0, 0, TimeSpan.Zero), 30);
        var neighbour = Tracked("b", new DateTimeOffset(2026, 5, 3, 9, 30, 0, TimeSpan.Zero), 30);
        h.Store.Entries.AddRange([entry, neighbour]);

        var result = await h.Service.UpdateAsync(entry, new(entry.WindowTitle, null, TimeSpan.FromHours(1)));

        Assert.True(result.IsSuccess);
        Assert.Equal("b", Assert.Single(result.OverlappingEntries).EntryId);
    }

    [Fact]
    public async Task UpdateAsync_GivenLegacyEntryEndingAtMidnight_AllowsTitleEdit()
    {
        var h = new Harness();
        var start = new DateTimeOffset(2026, 5, 3, 23, 30, 0, TimeSpan.Zero);
        var legacy = Tracked("legacy", start, 30);
        h.Store.Entries.Add(legacy);

        var result = await h.Service.UpdateAsync(legacy, new("Renamed", null, legacy.Duration));

        Assert.True(result.IsSuccess, result.Message);
        Assert.Equal(legacy.EndedAt, Assert.Single(h.Store.Entries).EndedAt);
    }

    [Fact]
    public async Task DeleteAsync_GivenCurrentRevision_RemovesExactlyThatEntry()
    {
        var h = new Harness();
        var keep = Tracked("keep", new DateTimeOffset(2026, 5, 3, 8, 0, 0, TimeSpan.Zero), 30);
        var drop = Tracked("drop", new DateTimeOffset(2026, 5, 3, 9, 0, 0, TimeSpan.Zero), 30);
        h.Store.Entries.AddRange([keep, drop]);

        var result = await h.Service.DeleteAsync(drop);

        Assert.True(result.IsSuccess);
        Assert.Equal(keep, Assert.Single(h.Store.Entries));
    }

    [Fact]
    public async Task DeleteAsync_GivenStaleRevision_ReportsConflict()
    {
        var h = new Harness();
        var entry = Tracked("e", new DateTimeOffset(2026, 5, 3, 9, 0, 0, TimeSpan.Zero), 30);
        h.Store.Entries.Add(entry with { Revision = 3 });

        var result = await h.Service.DeleteAsync(entry);

        Assert.Equal(WorklogOutcomeKind.Conflict, result.Kind);
        Assert.Single(h.Store.Entries);
    }

    [Fact]
    public async Task DeleteAsync_GivenMissingEntry_ReportsNotFound()
    {
        var h = new Harness();

        var result = await h.Service.DeleteAsync(Tracked("gone", new DateTimeOffset(2026, 5, 3, 9, 0, 0, TimeSpan.Zero), 30));

        Assert.Equal(WorklogOutcomeKind.NotFound, result.Kind);
    }

    [Theory]
    [InlineData(WorklogOutcomeKind.FileInUse, "in use")]
    [InlineData(WorklogOutcomeKind.UnsupportedSchema, "format")]
    [InlineData(WorklogOutcomeKind.MalformedData, "read safely")]
    public async Task DeleteAsync_GivenStoreFailure_ReturnsSpecificMessage(WorklogOutcomeKind kind, string fragment)
    {
        var h = new Harness();
        h.Store.NextOutcome = new WorklogOutcome(kind, "raw");

        var result = await h.Service.DeleteAsync(Tracked("e", new DateTimeOffset(2026, 5, 3, 9, 0, 0, TimeSpan.Zero), 30));

        Assert.Equal(kind, result.Kind);
        Assert.Contains(fragment, result.Message);
    }

    [Fact]
    public async Task Changes_PublishWorklogChangedWithTodayFlag()
    {
        var h = new Harness();
        var today = Tracked("today", new DateTimeOffset(2026, 5, 4, 8, 0, 0, TimeSpan.Zero), 30);
        var old = Tracked("old", new DateTimeOffset(2026, 5, 3, 8, 0, 0, TimeSpan.Zero), 30);
        h.Store.Entries.AddRange([today, old]);

        await h.Service.AddManualAsync(new(Today, new TimeOnly(9, 0), TimeSpan.FromMinutes(30), null, null));
        await h.Service.UpdateAsync(old, new("x", null, old.Duration));
        await h.Service.DeleteAsync(today);

        Assert.Equal(
            [(Today, true), (Yesterday, false), (Today, true)],
            h.Events.Select(e => (e.Day, e.IsToday)));
    }

    private static TimeZoneInfo Vienna() => TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "Central European Standard Time" : "Europe/Vienna");

    private static TimeEntry Tracked(string id, DateTimeOffset start, int minutes) => new(
        id, "session-" + id, start, start.AddMinutes(minutes), "Code", "Window", null, ProjectAssignmentSource.Unassigned, null,
        ActivityKind.Active, EndReason.ApplicationChange, CaptureSource.ActiveWindow, SourcePlatform.Windows, "device-1", 1, Now);

    private sealed class Harness
    {
        public Harness(Settings? settings = null, TimeZoneInfo? timeZone = null, DateTimeOffset? now = null)
        {
            this.Store = new MemoryStore();
            var identity = new InstallationIdentity();
            identity.Initialize("device-1");
            var bus = new EventBus();
            bus.Subscribe<WorklogChangedEvent>(this.Events.Add);
            this.Service = new WorklogEditingService(
                this.Store,
                new FixedSettingsProvider(settings ?? new Settings()),
                identity,
                new PlatformStub(),
                new Clock(now ?? Now, timeZone ?? TimeZoneInfo.Utc),
                bus,
                NullLogger.Instance);
        }

        public MemoryStore Store { get; }

        public WorklogEditingService Service { get; }

        public List<WorklogChangedEvent> Events { get; } = [];
    }

    private sealed class Clock(DateTimeOffset utcNow, TimeZoneInfo timeZone) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => timeZone;

        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class PlatformStub : ISourcePlatformProvider
    {
        public SourcePlatform GetCurrentPlatform() => SourcePlatform.Windows;
    }

    private sealed class MemoryStore : IWorklogStore
    {
        public List<TimeEntry> Entries { get; } = [];

        public WorklogOutcome? NextOutcome { get; set; }

        public int PatchCalls { get; private set; }

        public Task<WorklogOutcome> AppendAsync(IReadOnlyCollection<TimeEntry> entries, CancellationToken cancellationToken = default)
        {
            if (this.NextOutcome is { } forced)
            {
                return Task.FromResult(forced);
            }

            this.Entries.AddRange(entries);
            return Task.FromResult(WorklogOutcome.Success());
        }

        public Task<WorklogReadResult> GetAsync(string entryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorklogReadResult(WorklogOutcome.Success(), this.Entries.Where(e => e.EntryId == entryId).ToList()));

        public Task<WorklogReadResult> QueryAsync(WorklogQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorklogReadResult(
                WorklogOutcome.Success(),
                this.Entries.Where(e => e.StartedAt < query.EndExclusive && e.EndedAt > query.StartInclusive).OrderBy(e => e.StartedAt).ToList()));

        public Task<WorklogOutcome> PatchAsync(string entryId, int expectedRevision, WorklogPatch patch, CancellationToken cancellationToken = default)
        {
            this.PatchCalls++;
            var index = this.Entries.FindIndex(e => e.EntryId == entryId);
            if (index < 0)
            {
                return Task.FromResult(new WorklogOutcome(WorklogOutcomeKind.NotFound));
            }

            if (this.Entries[index].Revision != expectedRevision)
            {
                return Task.FromResult(new WorklogOutcome(WorklogOutcomeKind.Conflict, "Revision does not match."));
            }

            this.Entries[index] = this.Entries[index] with
            {
                StartedAt = patch.StartedAt,
                EndedAt = patch.EndedAt,
                AppName = patch.AppName,
                WindowTitle = patch.WindowTitle,
                ProjectTag = patch.ProjectTag,
                ProjectAssignmentSource = patch.ProjectAssignmentSource,
                ProjectRuleId = patch.ProjectRuleId,
                Revision = expectedRevision + 1,
            };
            return Task.FromResult(WorklogOutcome.Success());
        }

        public Task<WorklogOutcome> DeleteAsync(string entryId, int expectedRevision, CancellationToken cancellationToken = default)
        {
            if (this.NextOutcome is { } forced)
            {
                return Task.FromResult(forced);
            }

            var index = this.Entries.FindIndex(e => e.EntryId == entryId);
            if (index < 0)
            {
                return Task.FromResult(new WorklogOutcome(WorklogOutcomeKind.NotFound));
            }

            if (this.Entries[index].Revision != expectedRevision)
            {
                return Task.FromResult(new WorklogOutcome(WorklogOutcomeKind.Conflict, "Revision does not match."));
            }

            this.Entries.RemoveAt(index);
            return Task.FromResult(WorklogOutcome.Success());
        }
    }
}
