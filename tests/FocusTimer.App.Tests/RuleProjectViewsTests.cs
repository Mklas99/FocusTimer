namespace FocusTimer.App.Tests;

using FocusTimer.App.ViewModels;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

/// <summary>Entries, Timeline, and Summary all show the project resolved from rules, and follow rule edits on refresh.</summary>
public class RuleProjectViewsTests
{
    private static readonly DateTimeOffset Noon = new(2026, 5, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task EntriesTimelineAndSummaryAgreeOnRuleResolvedProject_AndFollowRuleEditsOnRefresh()
    {
        var clock = new MutableClock(Noon);
        var store = new MemoryWorklogStore();
        store.Add(WorklogTestData.Tracked("a", new DateTimeOffset(2026, 5, 4, 8, 0, 0, TimeSpan.Zero), 30, app: "code"));
        store.Add(WorklogTestData.Tracked("b", new DateTimeOffset(2026, 5, 4, 9, 0, 0, TimeSpan.Zero), 10, app: "mail"));
        var rules = new ProjectRuleStore();
        var resolver = new RuleProjectResolver(rules);
        var groupings = new WorklogGroupingRegistry([new ApplicationGrouping(), new ProjectGrouping(), new WindowGrouping()]);
        var window = new WorklogWindowViewModel(
            new WorklogEntriesViewModel(store, clock, null, resolver),
            new WorklogSummaryViewModel(new WorklogSummaryService(store, groupings, resolver, new Log()), groupings, clock),
            new SettingsStub(),
            clock,
            null,
            groupings,
            resolver);
        await window.OpenAsync();

        Assert.All(window.Entries.AllRows, r => Assert.Equal(WorklogEntryRowViewModel.EmptyValueText, r.ProjectText));

        rules.Update(new[] { new ProjectRule("code", null, "Alpha") });
        await window.RefreshAsync();

        Assert.Equal("Alpha", window.Entries.AllRows.Single(r => r.Entry.AppName == "code").ProjectText);
        Assert.Equal(WorklogEntryRowViewModel.EmptyValueText, window.Entries.AllRows.Single(r => r.Entry.AppName == "mail").ProjectText);

        await window.SelectTabAsync(WorklogTab.Summary);
        window.Summary.SelectedGrouping = window.Summary.Groupings.Single(g => g.Id == "project");
        await window.Summary.RefreshAsync();
        Assert.Contains(window.Summary.Rows, r => r.Label == "Alpha");
        Assert.Equal("0h 40m", window.Summary.TotalText);

        await window.SelectTabAsync(WorklogTab.Timeline);
        window.Timeline.SelectedGrouping = window.Timeline.GroupingOptions.First(o => o.Id == "project");
        Assert.Contains(window.Timeline.Groups, g => g.Label == "Alpha");

        rules.Update(Array.Empty<ProjectRule>());
        await window.RefreshAsync();
        Assert.All(window.Entries.AllRows, r => Assert.Equal(WorklogEntryRowViewModel.EmptyValueText, r.ProjectText));
    }

    [Fact]
    public async Task ExplicitProjectIsKeptAndSearchMatchesResolvedProject()
    {
        var clock = new MutableClock(Noon);
        var store = new MemoryWorklogStore();
        var tagged = WorklogTestData.Tracked("t", new DateTimeOffset(2026, 5, 4, 8, 0, 0, TimeSpan.Zero), 30, app: "code") with
        {
            ProjectTag = "Mine",
            ProjectAssignmentSource = ProjectAssignmentSource.Session,
        };
        store.Add(tagged);
        store.Add(WorklogTestData.Tracked("u", new DateTimeOffset(2026, 5, 4, 9, 0, 0, TimeSpan.Zero), 10, app: "code"));
        var rules = new ProjectRuleStore();
        rules.Update(new[] { new ProjectRule("code", null, "Alpha") });
        var entries = new WorklogEntriesViewModel(store, clock, null, new RuleProjectResolver(rules));
        await entries.RefreshAsync();

        Assert.Equal("Mine", entries.AllRows.Single(r => r.Entry.EntryId == "t").ProjectText);
        Assert.Equal("Alpha", entries.AllRows.Single(r => r.Entry.EntryId == "u").ProjectText);
        entries.SearchText = "alpha";
        Assert.Equal(new[] { "u" }, entries.Rows.Select(r => r.Entry.EntryId));
    }

    private sealed class Log : IAppLogger
    {
        public void LogCritical(string message, Exception? ex = null) { }
        public void LogDebug(string message) { }
        public void LogError(string message, Exception? ex = null) { }
        public void LogInformation(string message) { }
        public void LogWarning(string message) { }
    }
}
