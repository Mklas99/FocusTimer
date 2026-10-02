namespace FocusTimer.App.HeadlessTests;

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FocusTimer.App.Services;
using FocusTimer.App.ViewModels;
using FocusTimer.App.Views;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

/// <summary>
/// Drives the real add form, overlap banner, and delete confirmation through their controls, so the
/// XAML bindings (two-way text, commands, visibility) are covered and not only the view models.
/// </summary>
public sealed class WorklogEntryFormTests
{
    public WorklogEntryFormTests() => HeadlessAvaloniaFixture.EnsureInitialized();

    [Fact]
    public async Task AddForm_FilledThroughItsControls_StoresTheEntryAndShowsTheOverlapBanner()
    {
        using var h = await Harness.OpenAsync();
        h.Store.Seed(h.Day.AddHours(10), 60);

        h.Vm.Entries.AddCommand.Execute(null);
        h.Pump();
        Assert.True(h.Vm.Entries.IsEditorOpen);
        Assert.True(h.Find<TextBox>(t => AutomationProperties.GetName(t) == "Duration").IsEffectivelyVisible);

        h.Find<TextBox>(t => AutomationProperties.GetName(t) == "Start time").Text = "10:30";
        h.Find<TextBox>(t => AutomationProperties.GetName(t) == "Duration").Text = "1h 15m";
        h.Find<TextBox>(t => AutomationProperties.GetName(t) == "Window").Text = "Planning";
        h.Find<AutoCompleteBox>(t => AutomationProperties.GetName(t) == "Project").Text = " Alpha ";
        Assert.Equal("1h 15m", h.Vm.Entries.Editor!.DurationText);
        Assert.Equal("10:30", h.Vm.Entries.Editor.StartText);
        h.Find<Button>(b => b.Content as string == "Save" && b.IsEffectivelyVisible).Command!.Execute(null);
        h.Pump();
        await h.Vm.Entries.PendingOperation;
        h.Pump();

        var manual = Assert.Single(h.Store.Entries, e => e.CaptureSource == CaptureSource.Manual);
        Assert.Equal(TimeSpan.FromMinutes(75), manual.Duration);
        Assert.Equal("Alpha", manual.ProjectTag);
        Assert.Equal("Planning", manual.WindowTitle);
        Assert.False(h.Vm.Entries.IsEditorOpen);
        var banner = h.Find<Border>(b => AutomationProperties.GetName(b) == "Overlap warning");
        Assert.True(banner.IsEffectivelyVisible);
        var dismiss = h.Find<Button>(b => AutomationProperties.GetName(b) == "Dismiss overlap warning");
        Assert.True(dismiss.Focusable);
        Assert.True(dismiss.IsTabStop);
        dismiss.Command!.Execute(null);
        h.Pump();
        Assert.False(banner.IsEffectivelyVisible);
        Assert.Equal(2, h.Vm.Entries.Rows.Count);
    }

    [Fact]
    public async Task SearchBox_FiltersTheTableAsYouType_AndTheClearButtonRestoresIt()
    {
        using var h = await Harness.OpenAsync();
        h.Store.Seed(h.Day.AddHours(9), 30);
        h.Store.Seed(h.Day.AddHours(11), 30, "seed2", "Chrome");
        await h.Vm.Entries.RefreshAsync();
        h.Pump();
        Assert.Equal(2, h.Window.GetVisualDescendants().OfType<ListBoxItem>().Count());

        h.Find<TextBox>(t => AutomationProperties.GetName(t) == "Search entries").Text = "chrome";
        h.Pump();

        Assert.Equal("chrome", h.Vm.Entries.SearchText);
        var listBox = h.Find<ListBox>(l => l.Classes.Contains("worklog-table"));
        Assert.Equal(1, listBox.ItemCount);
        Assert.Single(h.Window.GetVisualDescendants().OfType<ListBoxItem>(), i => i.IsEffectivelyVisible && i.Bounds.Height > 0);
        Assert.True(h.Find<TextBlock>(t => t.Text == "1 of 2 entries").IsEffectivelyVisible);
        var clear = h.Find<Button>(b => AutomationProperties.GetName(b) == "Clear search");
        Assert.True(clear.IsEffectivelyVisible);

        clear.Command!.Execute(null);
        h.Pump();

        Assert.Equal(string.Empty, h.Find<TextBox>(t => AutomationProperties.GetName(t) == "Search entries").Text);
        Assert.Equal(2, h.Find<ListBox>(l => l.Classes.Contains("worklog-table")).ItemCount);
        Assert.Equal(2, h.Window.GetVisualDescendants().OfType<ListBoxItem>().Count(i => i.IsEffectivelyVisible && i.Bounds.Height > 0));
    }

    [Fact]
    public async Task Search_MakesTheMatchedTextBoldAndUnderlinedInTheRealTable_AndClearingRestoresPlainText()
    {
        using var h = await Harness.OpenAsync();
        h.Store.Seed(h.Day.AddHours(9), 30);
        await h.Vm.Entries.RefreshAsync();
        h.Pump();
        var application = h.Find<FocusTimer.App.Controls.HighlightTextBlock>(t => t.SourceText == "Code");
        Assert.Equal("Code", application.Text);
        Assert.True(application.Inlines is null || application.Inlines.Count == 0);

        h.Find<TextBox>(t => AutomationProperties.GetName(t) == "Search entries").Text = "od";
        h.Pump();

        application = h.Find<FocusTimer.App.Controls.HighlightTextBlock>(t => t.SourceText == "Code" && t.IsEffectivelyVisible);
        var runs = application.Inlines!.OfType<Avalonia.Controls.Documents.Run>().ToList();
        Assert.Equal(["C", "od", "e"], runs.Select(r => r.Text));
        Assert.Equal("Code", string.Concat(runs.Select(r => r.Text)));
        var match = runs[1];
        Assert.Equal(Avalonia.Media.FontWeight.Bold, match.FontWeight);
        Assert.Same(Avalonia.Media.TextDecorations.Underline, match.TextDecorations);
        Assert.NotEqual(Avalonia.Media.FontWeight.Bold, runs[0].FontWeight);
        Assert.Null(runs[0].TextDecorations);

        h.Vm.Entries.ClearSearchCommand.Execute(null);
        h.Pump();

        application = h.Find<FocusTimer.App.Controls.HighlightTextBlock>(t => t.SourceText == "Code" && t.IsEffectivelyVisible);
        Assert.Equal("Code", application.Text);
        Assert.True(application.Inlines is null || application.Inlines.Count == 0);
    }

    [Fact]
    public async Task CtrlF_FocusesTheSearchBox_AndEscapeClearsTheSearch()
    {
        using var h = await Harness.OpenAsync();
        h.Store.Seed(h.Day.AddHours(9), 30);
        await h.Vm.Entries.RefreshAsync();
        h.Pump();
        var box = h.Find<TextBox>(t => AutomationProperties.GetName(t) == "Search entries");
        await h.Vm.SelectTabAsync(WorklogTab.Summary);
        h.Window.FindControl<TabControl>("Tabs")!.SelectedIndex = 2;
        h.Pump();

        h.Window.KeyPress(Avalonia.Input.Key.F, Avalonia.Input.RawInputModifiers.Control, Avalonia.Input.PhysicalKey.F, "f");
        h.Pump();
        Assert.True(box.IsFocused);
        Assert.Equal(0, h.Window.FindControl<TabControl>("Tabs")!.SelectedIndex);

        box.Text = "code";
        h.Pump();
        h.Window.KeyPress(Avalonia.Input.Key.Escape, Avalonia.Input.RawInputModifiers.None, Avalonia.Input.PhysicalKey.Escape, null);
        h.Pump();

        Assert.Equal(string.Empty, h.Vm.Entries.SearchText);
    }

    [Fact]
    public async Task TableShowsNoSourceColumn_AndTheDetailsShowExactTimeAndSource()
    {
        using var h = await Harness.OpenAsync();
        h.Store.Seed(h.Day.AddHours(9), 30);
        await h.Vm.Entries.RefreshAsync();
        h.Vm.Entries.SelectedRow = h.Vm.Entries.Rows[0];
        h.Pump();

        var headers = h.Window.GetVisualDescendants().OfType<TextBlock>()
            .Where(t => t.Classes.Contains("SettingLabel") && t.Parent is Grid { ColumnDefinitions.Count: 6 })
            .Select(t => t.Text)
            .ToList();

        Assert.Equal(["Start", "End", "Duration", "Application", "Window", "Project"], headers);
        Assert.DoesNotContain(headers, text => text == "Source");
        Assert.True(h.Find<TextBlock>(t => t.Text == "Exact time").IsEffectivelyVisible);
        Assert.True(h.Find<TextBlock>(t => t.Text == "Tracked").IsEffectivelyVisible);
        Assert.Contains(":00", h.Find<TextBlock>(t => t.Text?.Contains('(') == true && t.Text.Contains('\u2013')).Text);
    }

    [Fact]
    public async Task AddForm_WithBadDuration_ShowsTheMessageNextToTheFields()
    {
        using var h = await Harness.OpenAsync();
        h.Vm.Entries.AddCommand.Execute(null);
        h.Pump();
        h.Find<TextBox>(t => AutomationProperties.GetName(t) == "Start time").Text = "9:00";
        h.Find<TextBox>(t => AutomationProperties.GetName(t) == "Duration").Text = "banana";

        h.Find<Button>(b => b.Content as string == "Save" && b.IsEffectivelyVisible).Command!.Execute(null);
        h.Pump();

        var message = h.Find<TextBlock>(t => t.Text?.Contains("not a duration") == true);
        Assert.True(message.IsEffectivelyVisible);
        Assert.Contains("2h 30m", message.Text);
        Assert.True(h.Vm.Entries.IsEditorOpen);
    }

    [Fact]
    public async Task DeleteConfirmation_ShowsThePromptAndOnlyDeletesAfterConfirm()
    {
        using var h = await Harness.OpenAsync();
        h.Store.Seed(h.Day.AddHours(10), 60);
        await h.Vm.Entries.RefreshAsync();
        h.Pump();
        h.Vm.Entries.SelectedRow = h.Vm.Entries.Rows[0];
        h.Vm.Entries.DeleteCommand.Execute(null);
        h.Pump();

        var prompt = h.Find<TextBlock>(t => t.Text?.StartsWith("Delete this entry?", StringComparison.Ordinal) == true);
        Assert.True(prompt.IsEffectivelyVisible);
        Assert.Single(h.Store.Entries);

        h.Find<Button>(b => b.Content as string == "Cancel" && b.IsEffectivelyVisible).Command!.Execute(null);
        h.Pump();
        Assert.False(prompt.IsEffectivelyVisible);
        Assert.Single(h.Store.Entries);

        h.Vm.Entries.DeleteCommand.Execute(null);
        h.Pump();
        h.Find<Button>(b => b.Content as string == "Delete" && b.IsEffectivelyVisible && b.Command == h.Vm.Entries.ConfirmDeleteCommand).Command!.Execute(null);
        h.Pump();
        await Task.CompletedTask;

        Assert.Empty(h.Store.Entries);
    }

    private sealed class Harness : IDisposable
    {
        private Harness(WorklogWindow window, WorklogWindowViewModel vm, SeedStore store, DateTimeOffset day)
        {
            this.Window = window;
            this.Vm = vm;
            this.Store = store;
            this.Day = day;
        }

        public WorklogWindow Window { get; }

        public WorklogWindowViewModel Vm { get; }

        public SeedStore Store { get; }

        public DateTimeOffset Day { get; }

        public static async Task<Harness> OpenAsync()
        {
            var manager = new ThemeManager();
            manager.InitializeThemeResources();
            manager.ApplyTheme(new ThemeService().BuiltInThemes.First(t => t.ThemeName == "Light"));
            var store = new SeedStore();
            var identity = new InstallationIdentity();
            identity.Initialize("device-1");
            var service = new WorklogEditingService(
                store, new Provider(), identity, new SourcePlatformProvider(), TimeProvider.System, new EventBus(), new Log());
            var vm = new WorklogWindowViewModel(
                new WorklogEntriesViewModel(store, TimeProvider.System, service),
                new WorklogSummaryViewModel(
                    new EmptySummary(),
                    new WorklogGroupingRegistry([new ApplicationGrouping(), new ProjectGrouping(), new WindowGrouping()]),
                    TimeProvider.System),
                new Provider(),
                TimeProvider.System);
            var window = new WorklogWindow { DataContext = vm };
            window.Show();
            await vm.OpenAsync();
            var day = new DateTimeOffset(DateTime.Today, TimeZoneInfo.Local.GetUtcOffset(DateTime.Today));
            store.Day = day;
            var harness = new Harness(window, vm, store, day);
            harness.Pump();
            return harness;
        }

        public void Pump()
        {
            // Two passes: new list items are realized on the layout pass that follows a binding update.
            for (var pass = 0; pass < 2; pass++)
            {
                Dispatcher.UIThread.RunJobs();
                this.Window.UpdateLayout();
            }
        }

        public T Find<T>(Func<T, bool> match)
            where T : Visual =>
            this.Window.GetVisualDescendants().OfType<T>().First(match);

        public void Dispose() => this.Window.Close();
    }

    private sealed class Log : IAppLogger
    {
        public void LogCritical(string message, Exception? ex = null) { }

        public void LogDebug(string message) { }

        public void LogError(string message, Exception? ex = null) { }

        public void LogInformation(string message) { }

        public void LogWarning(string message) { }
    }

    private sealed class Provider : ISettingsProvider
    {
        public Task<Settings> LoadAsync() => Task.FromResult(new Settings());

        public Task SaveAsync(Settings settings) => Task.CompletedTask;
    }

    private sealed class EmptySummary : IWorklogSummaryService
    {
        public Task<WorklogSummary> SummarizeAsync(WorklogSummaryRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorklogSummary(request, WorklogOutcome.Success(), [], TimeSpan.Zero, []));
    }

    private sealed class SeedStore : IWorklogStore
    {
        private readonly List<TimeEntry> _entries = [];

        public DateTimeOffset Day { get; set; }

        public IReadOnlyList<TimeEntry> Entries => this._entries;

        public void Seed(DateTimeOffset start, int minutes, string id = "seed", string app = "Code") => this._entries.Add(new TimeEntry(
            id, id + "-session", start, start.AddMinutes(minutes), app, "Program.cs", null, ProjectAssignmentSource.Unassigned,
            null, ActivityKind.Active, EndReason.ApplicationChange, CaptureSource.ActiveWindow, SourcePlatform.Windows, "device-1", 1,
            DateTimeOffset.UtcNow));

        public Task<WorklogOutcome> AppendAsync(IReadOnlyCollection<TimeEntry> entries, CancellationToken cancellationToken = default)
        {
            this._entries.AddRange(entries);
            return Task.FromResult(WorklogOutcome.Success());
        }

        public Task<WorklogReadResult> GetAsync(string entryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorklogReadResult(WorklogOutcome.Success(), this._entries.Where(e => e.EntryId == entryId).ToList()));

        public Task<WorklogReadResult> QueryAsync(WorklogQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorklogReadResult(
                WorklogOutcome.Success(),
                this._entries.Where(e => e.StartedAt < query.EndExclusive && e.EndedAt > query.StartInclusive).ToList()));

        public Task<WorklogOutcome> PatchAsync(string entryId, int expectedRevision, WorklogPatch patch, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorklogOutcome.Success());

        public Task<WorklogOutcome> DeleteAsync(string entryId, int expectedRevision, CancellationToken cancellationToken = default)
        {
            this._entries.RemoveAll(e => e.EntryId == entryId);
            return Task.FromResult(WorklogOutcome.Success());
        }
    }
}
