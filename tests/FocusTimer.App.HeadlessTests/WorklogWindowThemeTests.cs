namespace FocusTimer.App.HeadlessTests;

using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FocusTimer.App.Services;
using FocusTimer.App.ViewModels;
using FocusTimer.App.Views;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

/// <summary>
/// Loads the Worklog window under every built-in theme (including High Contrast) with real rows,
/// so a theme resource that is missing or mistyped in the XAML fails here instead of at runtime.
/// </summary>
public sealed class WorklogWindowThemeTests
{
    public WorklogWindowThemeTests() => HeadlessAvaloniaFixture.EnsureInitialized();

    [Fact]
    public async Task Window_UnderEveryBuiltInTheme_ShowsEntriesAndSummaryTabs()
    {
        var themes = new ThemeService().BuiltInThemes;
        Assert.Contains(themes, t => t.ThemeName == "High Contrast");
        var manager = new ThemeManager();
        manager.InitializeThemeResources();

        foreach (var theme in themes)
        {
            manager.ApplyTheme(theme);
            var viewModel = new WorklogWindowViewModel(
                new WorklogEntriesViewModel(new SampleStore(), TimeProvider.System),
                new WorklogSummaryViewModel(
                    new EmptySummary(),
                    new WorklogGroupingRegistry([new ApplicationGrouping(), new ProjectGrouping(), new WindowGrouping()]),
                    TimeProvider.System),
                new SettingsProvider(),
                TimeProvider.System);
            var window = new WorklogWindow { DataContext = viewModel };

            window.Show();
            await viewModel.OpenAsync();
            Dispatcher.UIThread.RunJobs();

            var rows = window.GetVisualDescendants().OfType<ListBoxItem>().ToList();
            Assert.True(rows.Count == 2, $"{theme.ThemeName}: expected 2 table rows, found {rows.Count}");
            Assert.All(rows, row => Assert.True(row.Bounds.Height >= 24, $"{theme.ThemeName}: row height {row.Bounds.Height}"));
            var tabs = window.FindControl<TabControl>("Tabs")!;
            Assert.Equal(["Entries", "Timeline", "Summary"], tabs.Items.OfType<TabItem>().Select(t => Avalonia.Automation.AutomationProperties.GetName(t)));
            tabs.SelectedIndex = 1;
            await viewModel.SelectTabAsync(WorklogTab.Timeline);
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            var panel = window.GetVisualDescendants().OfType<FocusTimer.App.Controls.TimelinePanel>().Single();
            Assert.True(panel.Children.Count == 2, $"{theme.ThemeName}: expected 2 timeline blocks, found {panel.Children.Count}");
            Assert.All(panel.Children, block => Assert.True(block.Bounds.Height >= FocusTimer.App.Controls.TimelinePanel.MinimumBlockHeight, theme.ThemeName));
            window.Close();
        }
    }

    private sealed class SettingsProvider : ISettingsProvider
    {
        public Task<Settings> LoadAsync() => Task.FromResult(new Settings());

        public Task SaveAsync(Settings settings) => Task.CompletedTask;
    }

    private sealed class EmptySummary : IWorklogSummaryService
    {
        public Task<WorklogSummary> SummarizeAsync(WorklogSummaryRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorklogSummary(request, WorklogOutcome.Success(), [], TimeSpan.Zero, []));
    }

    private sealed class SampleStore : IWorklogStore
    {
        public Task<WorklogOutcome> AppendAsync(IReadOnlyCollection<TimeEntry> entries, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorklogOutcome.Success());

        public Task<WorklogReadResult> GetAsync(string entryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorklogReadResult(WorklogOutcome.Success(), []));

        public Task<WorklogReadResult> QueryAsync(WorklogQuery query, CancellationToken cancellationToken = default)
        {
            var start = query.StartInclusive.AddHours(9);
            TimeEntry Entry(string id, int offsetMinutes, CaptureSource source) => new(
                id, "s" + id, start.AddMinutes(offsetMinutes), start.AddMinutes(offsetMinutes + 30), "App", "Window", null,
                ProjectAssignmentSource.Unassigned, null, ActivityKind.Active, EndReason.ApplicationChange, source,
                SourcePlatform.Windows, "device", 1, DateTimeOffset.UtcNow);
            return Task.FromResult(new WorklogReadResult(
                WorklogOutcome.Success(),
                [Entry("a", 0, CaptureSource.ActiveWindow), Entry("b", 45, CaptureSource.Manual)]));
        }

        public Task<WorklogOutcome> PatchAsync(string entryId, int expectedRevision, WorklogPatch patch, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorklogOutcome.Success());

        public Task<WorklogOutcome> DeleteAsync(string entryId, int expectedRevision, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorklogOutcome.Success());
    }
}
