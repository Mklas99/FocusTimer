namespace FocusTimer.App.Tests;

using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Windows.Input;
using FocusTimer.App.ViewModels;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;
using ReactiveUI;

public class WorklogEntryEditingViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 4, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 5, 4);
    private static readonly DateOnly Yesterday = new(2026, 5, 3);

    [Fact]
    public async Task Add_WithValidFields_StoresEntryClosesFormAndShowsItInTheTable()
    {
        var h = await Harness.CreateAsync(Yesterday);

        await h.OpenAddAsync();
        Assert.True(h.Vm.IsEditorOpen);
        Assert.True(h.Vm.IsDialogOpen);
        Assert.Equal("Manual entry", h.Vm.Editor!.ApplicationText);
        Assert.Equal(Yesterday, DateOnly.FromDateTime(h.Vm.Editor.Date!.Value));
        h.Vm.Editor.StartText = "14:00";
        h.Vm.Editor.DurationText = "2h 30m";
        h.Vm.Editor.WindowText = "Planning";
        h.Vm.Editor.ProjectText = "Alpha";
        await h.Vm.Editor.SaveAsync();
        await h.Vm.PendingOperation;

        Assert.False(h.Vm.IsEditorOpen);
        Assert.False(h.Vm.IsDialogOpen);
        var stored = Assert.Single(h.Store.Entries);
        Assert.Equal(TimeSpan.FromMinutes(150), stored.Duration);
        Assert.Equal("Planning", stored.WindowTitle);
        var row = Assert.Single(h.Vm.Rows);
        Assert.True(row.IsManual);
        Assert.Equal("14:00", row.StartText);
        Assert.Equal("Alpha", row.ProjectText);
    }

    [Theory]
    [InlineData("2.5h", 150)]
    [InlineData("45m", 45)]
    [InlineData("1h", 60)]
    public async Task Add_AcceptsBothDurationNotations(string text, int minutes)
    {
        var h = await Harness.CreateAsync(Yesterday);
        await h.OpenAddAsync();
        h.Vm.Editor!.StartText = "9:00";
        h.Vm.Editor.DurationText = text;

        await h.Vm.Editor.SaveAsync();

        Assert.Equal(TimeSpan.FromMinutes(minutes), Assert.Single(h.Store.Entries).Duration);
    }

    [Theory]
    [InlineData("soon", "14:00", "duration")]
    [InlineData("", "14:00", "duration")]
    [InlineData("1h", "25:99", "start time")]
    [InlineData("1h", "", "start time")]
    public async Task Add_WithUnreadableText_ShowsMessageWithExampleAndKeepsFormOpen(string duration, string start, string expected)
    {
        var h = await Harness.CreateAsync(Yesterday);
        await h.OpenAddAsync();
        h.Vm.Editor!.StartText = start;
        h.Vm.Editor.DurationText = duration;

        await h.Vm.Editor.SaveAsync();

        Assert.True(h.Vm.Editor.HasError);
        Assert.Contains(expected, h.Vm.Editor.ErrorText, StringComparison.OrdinalIgnoreCase);
        Assert.True(h.Vm.IsEditorOpen);
        Assert.Empty(h.Store.Entries);
        if (expected == "duration")
        {
            Assert.Contains("2h 30m", h.Vm.Editor.ErrorText);
        }
    }

    [Fact]
    public async Task Add_PastTheEndOfTheDay_ShowsTheServiceMessageAndKeepsFormOpen()
    {
        var h = await Harness.CreateAsync(Yesterday);
        await h.OpenAddAsync();
        h.Vm.Editor!.StartText = "23:00";
        h.Vm.Editor.DurationText = "2h";

        await h.Vm.Editor.SaveAsync();

        Assert.Contains("23:59", h.Vm.Editor.ErrorText);
        Assert.True(h.Vm.IsEditorOpen);
        Assert.Empty(h.Store.Entries);
    }

    [Fact]
    public async Task Add_ProjectSuggestions_ListDistinctProjectsOfTheSelectedDayAndToday()
    {
        var h = await Harness.CreateAsync(Yesterday);
        h.Store.Add(
            WorklogTestData.Tracked("a", new DateTimeOffset(2026, 5, 3, 9, 0, 0, TimeSpan.Zero), 30) with { ProjectTag = "Beta" },
            WorklogTestData.Tracked("b", new DateTimeOffset(2026, 5, 3, 10, 0, 0, TimeSpan.Zero), 30) with { ProjectTag = "beta" },
            WorklogTestData.Tracked("c", new DateTimeOffset(2026, 5, 4, 9, 0, 0, TimeSpan.Zero), 30) with { ProjectTag = "Alpha" },
            WorklogTestData.Tracked("d", new DateTimeOffset(2026, 5, 4, 10, 0, 0, TimeSpan.Zero), 30) with { ProjectTag = "  " },
            WorklogTestData.Tracked("e", new DateTimeOffset(2026, 5, 2, 10, 0, 0, TimeSpan.Zero), 30) with { ProjectTag = "Old" });
        await h.Vm.RefreshAsync();

        await h.OpenAddAsync();

        Assert.Equal(["Alpha", "Beta"], h.Vm.Editor!.ProjectSuggestions);
    }

    [Fact]
    public async Task Add_ProjectTypedOrChosen_BothAreStoredTrimmed()
    {
        var h = await Harness.CreateAsync(Yesterday);
        await h.OpenAddAsync();
        h.Vm.Editor!.StartText = "9:00";
        h.Vm.Editor.DurationText = "30m";
        h.Vm.Editor.ProjectText = "  Brand new project  ";
        await h.Vm.Editor.SaveAsync();
        await h.Vm.PendingOperation;
        await h.OpenAddAsync();
        h.Vm.Editor!.StartText = "10:00";
        h.Vm.Editor.DurationText = "30m";
        Assert.Contains("Brand new project", h.Vm.Editor.ProjectSuggestions);
        h.Vm.Editor.ProjectText = h.Vm.Editor.ProjectSuggestions[0];
        await h.Vm.Editor.SaveAsync();

        Assert.Equal(["Brand new project", "Brand new project"], h.Store.Entries.Select(e => e.ProjectTag));
    }

    [Fact]
    public async Task Add_ProjectOver100Characters_ShowsLimitMessage()
    {
        var h = await Harness.CreateAsync(Yesterday);
        await h.OpenAddAsync();
        h.Vm.Editor!.StartText = "9:00";
        h.Vm.Editor.DurationText = "30m";
        h.Vm.Editor.ProjectText = new string('p', 101);

        await h.Vm.Editor.SaveAsync();

        Assert.Contains("100", h.Vm.Editor.ErrorText);
        Assert.Empty(h.Store.Entries);
    }

    [Fact]
    public async Task Add_WithoutDateOrWithFutureDate_ShowsMessage()
    {
        var h = await Harness.CreateAsync(Yesterday);
        await h.OpenAddAsync();
        h.Vm.Editor!.StartText = "9:00";
        h.Vm.Editor.DurationText = "30m";
        h.Vm.Editor.Date = null;

        await h.Vm.Editor.SaveAsync();
        Assert.Equal("Choose a date.", h.Vm.Editor.ErrorText);

        h.Vm.Editor.Date = new DateTime(2026, 5, 5);
        await h.Vm.Editor.SaveAsync();
        Assert.Contains("not started", h.Vm.Editor.ErrorText);
        Assert.Empty(h.Store.Entries);
    }

    [Fact]
    public async Task Cancel_ClosesTheFormAndChangesNothing()
    {
        var h = await Harness.CreateAsync(Yesterday);
        await h.OpenAddAsync();
        h.Vm.Editor!.StartText = "9:00";
        h.Vm.Editor.DurationText = "30m";

        h.Vm.Editor.CancelCommand.Execute(null);

        Assert.False(h.Vm.IsEditorOpen);
        Assert.False(h.Vm.IsDialogOpen);
        Assert.Empty(h.Store.Entries);
    }

    [Fact]
    public async Task Commands_AreDisabledWhileAFormIsOpen_AndEditNeedsASelection()
    {
        var h = await Harness.CreateAsync(Yesterday);
        h.Store.Add(WorklogTestData.Tracked("a", new DateTimeOffset(2026, 5, 3, 9, 0, 0, TimeSpan.Zero), 30));
        await h.Vm.RefreshAsync();
        Assert.True(h.CanExecute(h.Vm.AddCommand));
        Assert.False(h.CanExecute(h.Vm.EditCommand));
        Assert.False(h.CanExecute(h.Vm.DeleteCommand));

        h.Vm.SelectedRow = h.Vm.Rows[0];
        Assert.True(h.CanExecute(h.Vm.EditCommand));
        Assert.True(h.CanExecute(h.Vm.DeleteCommand));

        await h.OpenAddAsync();
        Assert.False(h.CanExecute(h.Vm.AddCommand));
        Assert.False(h.CanExecute(h.Vm.EditCommand));
        Assert.False(h.CanExecute(h.Vm.DeleteCommand));
    }

    [Fact]
    public void WithoutEditingService_TheTableIsReadOnly()
    {
        var vm = new WorklogEntriesViewModel(new MemoryWorklogStore(), new MutableClock(Now));

        Assert.False(vm.IsEditingAvailable);
        Assert.False(((ICommand)vm.AddCommand).CanExecute(null));
    }

    [Fact]
    public async Task Edit_ShowsApplicationAndStartReadOnlyWithCurrentValues()
    {
        var h = await Harness.CreateAsync(Yesterday);
        var entry = WorklogTestData.Tracked("e", new DateTimeOffset(2026, 5, 3, 9, 15, 0, TimeSpan.Zero), 90, "Chrome", "Mail");
        h.Store.Add(entry);
        await h.Vm.RefreshAsync();
        h.Vm.SelectedRow = h.Vm.Rows[0];

        await h.Run(h.Vm.EditCommand);

        var editor = h.Vm.Editor!;
        Assert.True(editor.IsEdit);
        Assert.Equal("Edit entry", editor.Title);
        Assert.Equal("Chrome", editor.ApplicationText);
        Assert.False(editor.IsStartEditable);
        Assert.True(editor.IsStartReadOnly);
        Assert.Equal("09:15:00", editor.StartText);
        Assert.Equal("1h 30m", editor.DurationText);
        Assert.Equal("Mail", editor.WindowText);
    }

    [Fact]
    public async Task Edit_Duration_ChangesTheEndAndKeepsTheStart()
    {
        var h = await Harness.CreateAsync(Yesterday);
        var start = new DateTimeOffset(2026, 5, 3, 9, 0, 0, TimeSpan.Zero);
        h.Store.Add(WorklogTestData.Tracked("e", start, 30));
        await h.Vm.RefreshAsync();
        h.Vm.SelectedRow = h.Vm.Rows[0];
        await h.Run(h.Vm.EditCommand);

        h.Vm.Editor!.DurationText = "20m";
        await h.Vm.Editor.SaveAsync();
        await h.Vm.PendingOperation;

        var stored = Assert.Single(h.Store.Entries);
        Assert.Equal(start, stored.StartedAt);
        Assert.Equal(start.AddMinutes(20), stored.EndedAt);
        Assert.Equal(2, stored.Revision);
        Assert.Equal("0h 20m", Assert.Single(h.Vm.Rows).DurationText);
    }

    [Fact]
    public async Task Edit_TitleAndProjectOnly_KeepsTheExactEndOfAnEntryWithSeconds()
    {
        var h = await Harness.CreateAsync(Yesterday);
        var start = new DateTimeOffset(2026, 5, 3, 9, 0, 0, TimeSpan.Zero);
        var entry = WorklogTestData.Tracked("e", start, 30) with { EndedAt = start.AddMinutes(30).AddSeconds(12) };
        h.Store.Add(entry);
        await h.Vm.RefreshAsync();
        h.Vm.SelectedRow = h.Vm.Rows[0];
        await h.Run(h.Vm.EditCommand);

        h.Vm.Editor!.WindowText = "Renamed";
        h.Vm.Editor.ProjectText = "Alpha";
        await h.Vm.Editor.SaveAsync();
        await h.Vm.PendingOperation;

        var stored = Assert.Single(h.Store.Entries);
        Assert.Equal(entry.EndedAt, stored.EndedAt);
        Assert.Equal("Renamed", stored.WindowTitle);
        Assert.Equal("Alpha", stored.ProjectTag);
        Assert.Equal(ProjectAssignmentSource.Editor, stored.ProjectAssignmentSource);
    }

    [Fact]
    public async Task Delete_AsksForConfirmationAndOnlyDeletesAfterConfirm()
    {
        var h = await Harness.CreateAsync(Yesterday);
        h.Store.Add(
            WorklogTestData.Tracked("keep", new DateTimeOffset(2026, 5, 3, 8, 0, 0, TimeSpan.Zero), 30),
            WorklogTestData.Tracked("drop", new DateTimeOffset(2026, 5, 3, 9, 0, 0, TimeSpan.Zero), 30));
        await h.Vm.RefreshAsync();
        h.Vm.SelectedRow = h.Vm.Rows[1];

        await h.Run(h.Vm.DeleteCommand);
        Assert.True(h.Vm.IsDeleteConfirmOpen);
        Assert.True(h.Vm.IsDialogOpen);
        Assert.Contains("09:00", h.Vm.DeletePrompt);
        Assert.Equal(2, h.Store.Entries.Count);

        await h.Run(h.Vm.CancelDeleteCommand);
        Assert.False(h.Vm.IsDeleteConfirmOpen);
        Assert.False(h.Vm.IsDialogOpen);
        Assert.Equal(2, h.Store.Entries.Count);
        Assert.Equal(0, h.Store.DeleteCalls);

        await h.Run(h.Vm.DeleteCommand);
        await h.Run(h.Vm.ConfirmDeleteCommand);

        Assert.Equal(["keep"], h.Store.Entries.Select(e => e.EntryId));
        Assert.Equal(["keep"], h.Vm.Rows.Select(r => r.Entry.EntryId));
        Assert.False(h.Vm.IsDialogOpen);
    }

    [Fact]
    public async Task Add_OverlappingATrackedEntry_SavesAndShowsDismissibleWarning()
    {
        var h = await Harness.CreateAsync(Yesterday);
        h.Store.Add(WorklogTestData.Tracked("t", new DateTimeOffset(2026, 5, 3, 14, 30, 0, TimeSpan.Zero), 60));
        await h.Vm.RefreshAsync();

        await h.OpenAddAsync();
        h.Vm.Editor!.StartText = "14:00";
        h.Vm.Editor.DurationText = "1h";
        await h.Vm.Editor.SaveAsync();
        await h.Vm.PendingOperation;

        Assert.Equal(2, h.Store.Entries.Count);
        Assert.True(h.Vm.HasOverlapWarning);
        Assert.Contains("14:30", h.Vm.OverlapWarning);
        Assert.Contains("counted twice", h.Vm.OverlapWarning);

        await h.Run(h.Vm.DismissOverlapWarningCommand);

        Assert.False(h.Vm.HasOverlapWarning);
        Assert.Equal(string.Empty, h.Vm.OverlapWarning);
    }

    [Fact]
    public async Task Warning_IsReplacedByTheNextSave_AndNeverBlocksOtherActions()
    {
        var h = await Harness.CreateAsync(Yesterday);
        h.Store.Add(WorklogTestData.Tracked("t", new DateTimeOffset(2026, 5, 3, 14, 30, 0, TimeSpan.Zero), 60));
        await h.Vm.RefreshAsync();
        await h.OpenAddAsync();
        h.Vm.Editor!.StartText = "14:00";
        h.Vm.Editor.DurationText = "1h";
        await h.Vm.Editor.SaveAsync();
        await h.Vm.PendingOperation;
        Assert.True(h.Vm.HasOverlapWarning);
        Assert.True(h.CanExecute(h.Vm.AddCommand));

        await h.OpenAddAsync();
        h.Vm.Editor!.StartText = "6:00";
        h.Vm.Editor.DurationText = "30m";
        await h.Vm.Editor.SaveAsync();
        await h.Vm.PendingOperation;

        Assert.False(h.Vm.HasOverlapWarning);
        Assert.Equal(3, h.Store.Entries.Count);
    }

    [Fact]
    public async Task Warning_IsClearedWhenTheDayChanges()
    {
        var h = await Harness.CreateAsync(Yesterday);
        h.Store.Add(WorklogTestData.Tracked("t", new DateTimeOffset(2026, 5, 3, 14, 30, 0, TimeSpan.Zero), 60));
        await h.Vm.RefreshAsync();
        await h.OpenAddAsync();
        h.Vm.Editor!.StartText = "14:00";
        h.Vm.Editor.DurationText = "1h";
        await h.Vm.Editor.SaveAsync();
        await h.Vm.PendingOperation;
        await h.OpenAddAsync();

        h.Vm.SetDay(Yesterday.AddDays(-1));

        Assert.False(h.Vm.HasOverlapWarning);
        Assert.False(h.Vm.IsEditorOpen);
    }

    [Fact]
    public async Task Edit_WhenEntryChangedMeanwhile_ShowsMessageClosesFormAndReloads()
    {
        var h = await Harness.CreateAsync(Yesterday);
        var entry = WorklogTestData.Tracked("e", new DateTimeOffset(2026, 5, 3, 9, 0, 0, TimeSpan.Zero), 30);
        h.Store.Add(entry);
        await h.Vm.RefreshAsync();
        h.Vm.SelectedRow = h.Vm.Rows[0];
        await h.Run(h.Vm.EditCommand);
        h.Store.ReplaceFirst(entry with { Revision = 5, WindowTitle = "Changed elsewhere" });
        h.Vm.Editor!.WindowText = "Mine";

        await h.Vm.Editor.SaveAsync();
        await h.Vm.PendingOperation;

        Assert.False(h.Vm.IsEditorOpen);
        Assert.True(h.Vm.HasActionError);
        Assert.Contains("changed elsewhere", h.Vm.ActionError);
        Assert.Equal("Changed elsewhere", Assert.Single(h.Store.Entries).WindowTitle);
        Assert.Equal("Changed elsewhere", Assert.Single(h.Vm.Rows).WindowText);
    }

    [Fact]
    public async Task Delete_WhenEntryAlreadyGone_ShowsMessageAndReloads()
    {
        var h = await Harness.CreateAsync(Yesterday);
        h.Store.Add(WorklogTestData.Tracked("e", new DateTimeOffset(2026, 5, 3, 9, 0, 0, TimeSpan.Zero), 30));
        await h.Vm.RefreshAsync();
        h.Vm.SelectedRow = h.Vm.Rows[0];
        await h.Run(h.Vm.DeleteCommand);
        h.Store.Clear();

        await h.Run(h.Vm.ConfirmDeleteCommand);

        Assert.Contains("no longer exists", h.Vm.ActionError);
        Assert.Empty(h.Vm.Rows);
        await h.Run(h.Vm.DismissActionErrorCommand);
        Assert.False(h.Vm.HasActionError);
    }

    [Theory]
    [InlineData(WorklogOutcomeKind.FileInUse, "in use")]
    [InlineData(WorklogOutcomeKind.UnsupportedSchema, "format")]
    [InlineData(WorklogOutcomeKind.MalformedData, "read safely")]
    [InlineData(WorklogOutcomeKind.IoFailure, "could not be written")]
    public async Task Save_WhenTheStoreFails_ShowsASpecificMessageInTheForm(WorklogOutcomeKind kind, string fragment)
    {
        var h = await Harness.CreateAsync(Yesterday);
        await h.OpenAddAsync();
        h.Vm.Editor!.StartText = "9:00";
        h.Vm.Editor.DurationText = "30m";
        h.Store.NextWriteOutcome = new WorklogOutcome(kind, "raw");

        await h.Vm.Editor.SaveAsync();

        Assert.Contains(fragment, h.Vm.Editor.ErrorText);
        Assert.True(h.Vm.IsEditorOpen);
        Assert.False(h.Vm.Editor.IsSaving);
    }

    [Fact]
    public async Task Delete_WhenTheStoreFails_ShowsAMessageAndKeepsTheEntry()
    {
        var h = await Harness.CreateAsync(Yesterday);
        h.Store.Add(WorklogTestData.Tracked("e", new DateTimeOffset(2026, 5, 3, 9, 0, 0, TimeSpan.Zero), 30));
        await h.Vm.RefreshAsync();
        h.Vm.SelectedRow = h.Vm.Rows[0];
        await h.Run(h.Vm.DeleteCommand);
        h.Store.NextWriteOutcome = new WorklogOutcome(WorklogOutcomeKind.FileInUse, "locked");

        await h.Run(h.Vm.ConfirmDeleteCommand);

        Assert.Contains("in use", h.Vm.ActionError);
        Assert.Single(h.Store.Entries);
    }

    [Fact]
    public async Task EditingToday_WhileTrackingAppends_KeepsBothChangesOrReportsConflict()
    {
        var h = await Harness.CreateAsync(Today);
        var entry = WorklogTestData.Tracked("e", new DateTimeOffset(2026, 5, 4, 8, 0, 0, TimeSpan.Zero), 30);
        h.Store.Add(entry);
        await h.Vm.RefreshAsync();
        h.Vm.SelectedRow = h.Vm.Rows[0];
        await h.Run(h.Vm.EditCommand);
        h.Vm.Editor!.WindowText = "Edited";

        // The tracker persists a new entry between loading the day and saving the edit.
        h.Store.BeforeWrite = () => h.Store.Add(WorklogTestData.Tracked("tracked", new DateTimeOffset(2026, 5, 4, 9, 0, 0, TimeSpan.Zero), 10));
        await h.Vm.Editor.SaveAsync();
        await h.Vm.PendingOperation;

        Assert.Equal(["e", "tracked"], h.Vm.Rows.Select(r => r.Entry.EntryId));
        Assert.Equal("Edited", h.Vm.Rows[0].Entry.WindowTitle);
        Assert.False(h.Vm.HasActionError);
    }

    private sealed class Harness
    {
        private Harness(WorklogEntriesViewModel vm, MemoryWorklogStore store)
        {
            this.Vm = vm;
            this.Store = store;
        }

        public WorklogEntriesViewModel Vm { get; }

        public MemoryWorklogStore Store { get; }

        public static async Task<Harness> CreateAsync(DateOnly day)
        {
            var clock = new MutableClock(Now);
            var store = new MemoryWorklogStore();
            var identity = new InstallationIdentity();
            identity.Initialize("device-1");
            var service = new WorklogEditingService(
                store, new SettingsStub(), identity, new SourcePlatformProvider(), clock, new EventBus(), new TestLogger());
            var vm = new WorklogEntriesViewModel(store, clock, service);
            vm.SetDay(day);
            await vm.RefreshAsync();
            return new Harness(vm, store);
        }

        public bool CanExecute(ICommand command) => command.CanExecute(null);

        public Task Run(ICommand command) => ((ReactiveCommand<Unit, Unit>)command).Execute().ToTask();

        public Task OpenAddAsync() => this.Run(this.Vm.AddCommand);
    }

    private sealed class TestLogger : FocusTimer.Core.Interfaces.IAppLogger
    {
        public void LogCritical(string message, Exception? ex = null) { }

        public void LogDebug(string message) { }

        public void LogError(string message, Exception? ex = null) { }

        public void LogInformation(string message) { }

        public void LogWarning(string message) { }
    }
}
