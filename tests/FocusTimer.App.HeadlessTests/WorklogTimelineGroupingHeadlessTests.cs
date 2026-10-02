namespace FocusTimer.App.HeadlessTests;

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FocusTimer.App.Controls;
using FocusTimer.App.Services;
using FocusTimer.App.ViewModels;
using FocusTimer.App.Views;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

/// <summary>
/// Drives the grouped timeline through its real controls: the Group by switch, one column per group, the column
/// headers, and sideways scrolling that keeps the hour labels and the headers where they belong.
/// </summary>
public sealed class WorklogTimelineGroupingHeadlessTests
{
    public WorklogTimelineGroupingHeadlessTests() => HeadlessAvaloniaFixture.EnsureInitialized();

    [Fact]
    public async Task GroupBySwitch_SplitsTheTimelineIntoColumnsWithHeaders_AndBackAgain()
    {
        var (window, viewModel) = await OpenTimelineAsync(applications: 3);
        var panel = window.GetVisualDescendants().OfType<TimelinePanel>().Single();
        Assert.Equal(1, panel.ColumnCount);
        Assert.Empty(window.GetVisualDescendants().OfType<TimelineColumnsPanel>().SelectMany(p => p.Children));

        var combo = window.GetVisualDescendants().OfType<ComboBox>().Single(c => AutomationProperties.GetName(c) == "Group the timeline by");
        combo.SelectedItem = viewModel.Timeline.GroupingOptions.Single(o => o.Id == "app");
        Pump(window);

        Assert.Equal("app", viewModel.Timeline.SelectedGrouping.Id);
        Assert.Equal(3, panel.ColumnCount);
        var headers = window.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("timeline-header")).ToList();
        Assert.Equal(3, headers.Count);
        Assert.All(headers, header => Assert.True(header.IsEffectivelyVisible));
        var texts = headers.Select(h => string.Join("|", h.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? string.Concat(t.Inlines!.OfType<Avalonia.Controls.Documents.Run>().Select(r => r.Text))))).ToList();
        Assert.Contains(texts, t => t.StartsWith("App 1|", StringComparison.Ordinal));
        Assert.Contains(texts, t => t.Contains("entry", StringComparison.Ordinal));

        // Each application's blocks sit in their own column and do not cover each other.
        var blocks = panel.Children.ToList();
        Assert.Equal(3, blocks.Count);
        Assert.Equal(3, blocks.Select(b => Math.Round(b.Bounds.X)).Distinct().Count());
        Assert.All(blocks, b => Assert.True(b.Bounds.Right <= panel.Bounds.Width + 0.5));

        combo.SelectedItem = viewModel.Timeline.GroupingOptions.Single(o => o.Id == WorklogTimelineViewModel.NoGroupingId);
        Pump(window);

        Assert.Equal(1, panel.ColumnCount);
        Assert.DoesNotContain(window.GetVisualDescendants().OfType<Border>(), b => b.Classes.Contains("timeline-header") && b.IsEffectivelyVisible);
        window.Close();
    }

    [Fact]
    public async Task ManyGroups_ScrollSidewaysWhileTheHourLabelsAndHeadersStayPut()
    {
        var (window, viewModel) = await OpenTimelineAsync(applications: 14);
        viewModel.Timeline.SelectedGrouping = viewModel.Timeline.GroupingOptions.Single(o => o.Id == "app");
        Pump(window);
        var scroller = window.GetVisualDescendants().OfType<ScrollViewer>().Single(s => s.Name == "Scroller");
        var header = window.GetVisualDescendants().OfType<ScrollViewer>().Single(s => s.Name == "HeaderScroller");
        var gutter = window.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "Gutter");
        var panel = window.GetVisualDescendants().OfType<TimelinePanel>().Single();

        Assert.True(panel.Bounds.Width >= (14 * TimelinePanel.MinColumnWidth) - 0.5, $"panel width {panel.Bounds.Width}");
        Assert.True(scroller.Extent.Width > scroller.Viewport.Width, $"extent {scroller.Extent.Width} viewport {scroller.Viewport.Width}");

        scroller.Offset = new Vector(300, scroller.Offset.Y);
        Pump(window);

        Assert.Equal(300, scroller.Offset.X, 1);
        Assert.Equal(300, header.Offset.X, 1);
        var transform = Assert.IsType<TranslateTransform>(gutter.RenderTransform);
        Assert.Equal(300, transform.X, 1);

        scroller.Offset = new Vector(0, scroller.Offset.Y);
        Pump(window);
        Assert.Equal(0, header.Offset.X, 1);
        Assert.Equal(0, ((TranslateTransform)gutter.RenderTransform!).X, 1);
        window.Close();
    }

    [Fact]
    public async Task FewGroups_FillTheWidthWithoutSidewaysScrolling()
    {
        var (window, viewModel) = await OpenTimelineAsync(applications: 3);
        viewModel.Timeline.SelectedGrouping = viewModel.Timeline.GroupingOptions.Single(o => o.Id == "app");
        Pump(window);
        var scroller = window.GetVisualDescendants().OfType<ScrollViewer>().Single(s => s.Name == "Scroller");
        var panel = window.GetVisualDescendants().OfType<TimelinePanel>().Single();

        Assert.True(scroller.Extent.Width <= scroller.Viewport.Width + 0.5, $"extent {scroller.Extent.Width} viewport {scroller.Viewport.Width}");
        Assert.True(panel.Bounds.Width > 3 * TimelinePanel.MinColumnWidth);
        window.Close();
    }

    [Fact]
    public async Task ZoomStillWorksInAGroupedTimeline()
    {
        var (window, viewModel) = await OpenTimelineAsync(applications: 3);
        viewModel.Timeline.SelectedGrouping = viewModel.Timeline.GroupingOptions.Single(o => o.Id == "app");
        Pump(window);
        var panel = window.GetVisualDescendants().OfType<TimelinePanel>().Single();

        window.MouseWheel(new Point(450, 400), new Vector(0, 1), Avalonia.Input.RawInputModifiers.Control);
        Pump(window);

        Assert.Equal(75, viewModel.Timeline.HourHeight, 3);
        Assert.Equal(75, panel.HourHeight, 3);
        Assert.Equal(24 * 75, panel.Bounds.Height, 1);
        Assert.Equal(3, panel.ColumnCount);
        window.Close();
    }

    private static void Pump(Window window)
    {
        for (var pass = 0; pass < 3; pass++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    private static async Task<(WorklogWindow Window, WorklogWindowViewModel ViewModel)> OpenTimelineAsync(int applications)
    {
        var registry = new WorklogGroupingRegistry([new ApplicationGrouping(), new ProjectGrouping(), new WindowGrouping()]);
        var viewModel = new WorklogWindowViewModel(
            new WorklogEntriesViewModel(new Store(applications), TimeProvider.System),
            new WorklogSummaryViewModel(new Summary(), registry, TimeProvider.System),
            new Provider(),
            TimeProvider.System,
            null,
            registry);
        var window = new WorklogWindow { DataContext = viewModel, Width = 900, Height = 700 };
        window.Show();
        await viewModel.OpenAsync();
        await viewModel.SelectTabAsync(WorklogTab.Timeline);
        window.FindControl<TabControl>("Tabs")!.SelectedIndex = 1;
        Pump(window);
        return (window, viewModel);
    }

    private sealed class Provider : ISettingsProvider
    {
        public Task<Settings> LoadAsync() => Task.FromResult(new Settings());

        public Task SaveAsync(Settings settings) => Task.CompletedTask;
    }

    private sealed class Summary : IWorklogSummaryService
    {
        public Task<WorklogSummary> SummarizeAsync(WorklogSummaryRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorklogSummary(request, WorklogOutcome.Success(), [], TimeSpan.Zero, []));
    }

    private sealed class Store(int applications) : IWorklogStore
    {
        public Task<WorklogOutcome> AppendAsync(IReadOnlyCollection<TimeEntry> entries, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorklogOutcome.Success());

        public Task<WorklogReadResult> GetAsync(string entryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorklogReadResult(WorklogOutcome.Success(), []));

        public Task<WorklogReadResult> QueryAsync(WorklogQuery query, CancellationToken cancellationToken = default)
        {
            var day = query.StartInclusive;
            var entries = Enumerable.Range(1, applications).Select(i => new TimeEntry(
                "e" + i, "s" + i, day.AddHours(9).AddMinutes(i * 5), day.AddHours(9).AddMinutes((i * 5) + 40), "App " + i, "Window " + i, null,
                ProjectAssignmentSource.Unassigned, null, ActivityKind.Active, EndReason.ApplicationChange, CaptureSource.ActiveWindow,
                SourcePlatform.Windows, "device", 1, DateTimeOffset.UtcNow)).ToList();
            return Task.FromResult(new WorklogReadResult(WorklogOutcome.Success(), entries));
        }

        public Task<WorklogOutcome> PatchAsync(string entryId, int expectedRevision, WorklogPatch patch, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorklogOutcome.Success());

        public Task<WorklogOutcome> DeleteAsync(string entryId, int expectedRevision, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorklogOutcome.Success());
    }
}
