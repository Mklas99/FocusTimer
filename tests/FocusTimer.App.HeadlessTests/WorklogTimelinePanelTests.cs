namespace FocusTimer.App.HeadlessTests;

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FocusTimer.App.Controls;
using FocusTimer.App.ViewModels;
using FocusTimer.App.Views;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

/// <summary>
/// Checks the real timeline panel: block positions follow the entry times, overlapping entries are drawn side by
/// side without covering each other, and a gap stays empty.
/// </summary>
public sealed class WorklogTimelinePanelTests
{
    public WorklogTimelinePanelTests() => HeadlessAvaloniaFixture.EnsureInitialized();

    [Fact]
    public async Task Panel_PlacesBlocksByTimeAndSplitsOverlappingOnesIntoLanes()
    {
        var store = new Store();
        var viewModel = new WorklogWindowViewModel(
            new WorklogEntriesViewModel(store, TimeProvider.System),
            new WorklogSummaryViewModel(new Summary(), new WorklogGroupingRegistry([new ApplicationGrouping()]), TimeProvider.System),
            new Provider(),
            TimeProvider.System);
        var window = new WorklogWindow { DataContext = viewModel, Width = 900, Height = 700 };
        window.Show();
        await viewModel.OpenAsync();
        await viewModel.SelectTabAsync(WorklogTab.Timeline);
        window.FindControl<TabControl>("Tabs")!.SelectedIndex = 1;
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        var panel = window.GetVisualDescendants().OfType<TimelinePanel>().Single();
        var blocks = panel.Children.OrderBy(c => c.Bounds.Y).ThenBy(c => c.Bounds.X).ToList();

        Assert.Equal(4, blocks.Count);
        Assert.Equal(panel.AxisHeight, panel.Bounds.Height);
        var nine = blocks[0];
        var nineThirty = blocks[1];
        var afternoon = blocks[3];
        Assert.Equal(9 * panel.HourHeight, nine.Bounds.Y, 1);
        Assert.Equal(1.5 * panel.HourHeight, nine.Bounds.Height, 1);
        Assert.Equal(9.5 * panel.HourHeight, nineThirty.Bounds.Y, 1);

        // The two overlapping entries sit side by side and do not cover each other.
        var overlapping = blocks.Take(3).ToList();
        Assert.Equal(2, overlapping.Select(b => Math.Round(b.Bounds.X)).Distinct().Count());
        Assert.False(overlapping[0].Bounds.Intersects(overlapping[1].Bounds));

        // Entries without overlap use the full width; the gap before 15:00 holds no block.
        Assert.Equal(15 * panel.HourHeight, afternoon.Bounds.Y, 1);
        Assert.True(afternoon.Bounds.Width > overlapping[0].Bounds.Width);
        Assert.DoesNotContain(blocks, b => b.Bounds.Y > nine.Bounds.Bottom + 60 && b.Bounds.Bottom < afternoon.Bounds.Y);
        window.Close();
    }

    [Fact]
    public async Task CtrlWheel_ZoomsTheHoursAndPlainWheelDoesNot()
    {
        var (window, viewModel) = await OpenTimelineAsync();
        var panel = window.GetVisualDescendants().OfType<TimelinePanel>().Single();
        var firstBlockHeight = panel.Children.OrderBy(c => c.Bounds.Y).First().Bounds.Height;
        var point = new Avalonia.Point(450, 350);

        window.MouseWheel(point, new Avalonia.Vector(0, 1));
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.Equal(60, viewModel.Timeline.HourHeight);

        window.MouseWheel(point, new Avalonia.Vector(0, 1), Avalonia.Input.RawInputModifiers.Control);
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        Assert.Equal(75, viewModel.Timeline.HourHeight, 3);
        Assert.Equal(75, panel.HourHeight, 3);
        Assert.Equal(24 * 75, panel.Bounds.Height, 1);
        Assert.InRange(panel.Children.OrderBy(c => c.Bounds.Y).First().Bounds.Height, (firstBlockHeight * 1.25) - 1, (firstBlockHeight * 1.25) + 1);

        window.MouseWheel(point, new Avalonia.Vector(0, -1), Avalonia.Input.RawInputModifiers.Control);
        window.MouseWheel(point, new Avalonia.Vector(0, -1), Avalonia.Input.RawInputModifiers.Control);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(48, viewModel.Timeline.HourHeight, 3);
        window.Close();
    }

    [Fact]
    public async Task CtrlPlusMinusAndZero_ZoomFromTheKeyboard()
    {
        var (window, viewModel) = await OpenTimelineAsync();
        window.FindControl<ScrollViewer>("Scroller")?.Focus();
        var scroller = window.GetVisualDescendants().OfType<ScrollViewer>().First(s => s.Name == "Scroller");
        scroller.Focus();

        window.KeyPress(Avalonia.Input.Key.OemPlus, Avalonia.Input.RawInputModifiers.Control, Avalonia.Input.PhysicalKey.Equal, "=");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(75, viewModel.Timeline.HourHeight, 3);

        window.KeyPress(Avalonia.Input.Key.OemMinus, Avalonia.Input.RawInputModifiers.Control, Avalonia.Input.PhysicalKey.Minus, "-");
        window.KeyPress(Avalonia.Input.Key.OemMinus, Avalonia.Input.RawInputModifiers.Control, Avalonia.Input.PhysicalKey.Minus, "-");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(48, viewModel.Timeline.HourHeight, 3);

        window.KeyPress(Avalonia.Input.Key.D0, Avalonia.Input.RawInputModifiers.Control, Avalonia.Input.PhysicalKey.Digit0, "0");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(60, viewModel.Timeline.HourHeight);
        window.Close();
    }

    [Fact]
    public async Task ZoomButtons_AreEnabledOnlyWithinTheLimits()
    {
        var (window, viewModel) = await OpenTimelineAsync();
        var zoomIn = window.GetVisualDescendants().OfType<Button>().First(b => Avalonia.Automation.AutomationProperties.GetName(b) == "Zoom in");
        var zoomOut = window.GetVisualDescendants().OfType<Button>().First(b => Avalonia.Automation.AutomationProperties.GetName(b) == "Zoom out");

        zoomIn.Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(75, viewModel.Timeline.HourHeight, 3);

        viewModel.Timeline.HourHeight = WorklogTimelineViewModel.MaxHourHeight;
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.False(zoomIn.IsEnabled);
        Assert.True(zoomOut.IsEnabled);
        window.Close();
    }

    private static async Task<(WorklogWindow Window, WorklogWindowViewModel ViewModel)> OpenTimelineAsync()
    {
        var viewModel = new WorklogWindowViewModel(
            new WorklogEntriesViewModel(new Store(), TimeProvider.System),
            new WorklogSummaryViewModel(new Summary(), new WorklogGroupingRegistry([new ApplicationGrouping()]), TimeProvider.System),
            new Provider(),
            TimeProvider.System);
        var window = new WorklogWindow { DataContext = viewModel, Width = 900, Height = 700 };
        window.Show();
        await viewModel.OpenAsync();
        await viewModel.SelectTabAsync(WorklogTab.Timeline);
        window.FindControl<TabControl>("Tabs")!.SelectedIndex = 1;
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
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

    private sealed class Store : IWorklogStore
    {
        public Task<WorklogOutcome> AppendAsync(IReadOnlyCollection<TimeEntry> entries, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorklogOutcome.Success());

        public Task<WorklogReadResult> GetAsync(string entryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorklogReadResult(WorklogOutcome.Success(), []));

        public Task<WorklogReadResult> QueryAsync(WorklogQuery query, CancellationToken cancellationToken = default)
        {
            var day = query.StartInclusive;
            TimeEntry Entry(string id, double startHour, int minutes, CaptureSource source) => new(
                id, "s" + id, day.AddHours(startHour), day.AddHours(startHour).AddMinutes(minutes), "App", "Window", null,
                ProjectAssignmentSource.Unassigned, null, ActivityKind.Active, EndReason.ApplicationChange, source,
                SourcePlatform.Windows, "device", 1, DateTimeOffset.UtcNow);
            return Task.FromResult(new WorklogReadResult(
                WorklogOutcome.Success(),
                [
                    Entry("a", 9, 90, CaptureSource.ActiveWindow),
                    Entry("b", 9.5, 60, CaptureSource.Manual),
                    Entry("c", 10.75, 30, CaptureSource.ActiveWindow),
                    Entry("d", 15, 60, CaptureSource.ActiveWindow),
                ]));
        }

        public Task<WorklogOutcome> PatchAsync(string entryId, int expectedRevision, WorklogPatch patch, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorklogOutcome.Success());

        public Task<WorklogOutcome> DeleteAsync(string entryId, int expectedRevision, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorklogOutcome.Success());
    }
}
