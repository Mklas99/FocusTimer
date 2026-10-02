namespace FocusTimer.App.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;
    using ReactiveUI;

    /// <summary>
    /// Shows the stored entries of one local day in start order. It has no dependency on the window that
    /// hosts it; the host sets <see cref="Day"/> and reloads.
    /// </summary>
    public class WorklogEntriesViewModel : ReactiveObject
    {
        private readonly IWorklogStore _store;
        private readonly TimeProvider _timeProvider;
        private readonly IWorklogEditingService? _editingService;
        private IReadOnlyList<WorklogEntryRowViewModel> _rows = [];
        private IReadOnlyList<TimeEntry> _loadedEntries = [];
        private WorklogEntryRowViewModel? _selectedRow;
        private SummaryViewStatus _status = SummaryViewStatus.Idle;
        private string _statusMessage = string.Empty;
        private string _warningText = string.Empty;
        private bool _isDialogOpen;
        private WorklogEntryEditorViewModel? _editor;
        private WorklogEntryRowViewModel? _pendingDelete;
        private string _overlapWarning = string.Empty;
        private string _actionError = string.Empty;
        private DateOnly? _earliestDay;
        private DateOnly _day;
        private CancellationTokenSource? _cts;
        private int _version;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorklogEntriesViewModel"/> class.
        /// </summary>
        /// <param name="store">The worklog to read.</param>
        /// <param name="timeProvider">The clock and time zone that decide the local day.</param>
        /// <param name="editingService">Adds, edits, and deletes entries; without it the table is read-only.</param>
        public WorklogEntriesViewModel(IWorklogStore store, TimeProvider timeProvider, IWorklogEditingService? editingService = null)
        {
            this._store = store;
            this._timeProvider = timeProvider;
            this._editingService = editingService;
            this._day = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
            this.RefreshCommand = ReactiveCommand.CreateFromTask(this.RefreshAsync);
            var canAdd = this.WhenAnyValue(x => x.IsEditingAvailable, x => x.IsDialogOpen, (available, open) => available && !open);
            var canChange = this.WhenAnyValue(x => x.IsEditingAvailable, x => x.IsDialogOpen, x => x.SelectedRow, (available, open, row) => available && !open && row is not null);
            this.AddCommand = ReactiveCommand.CreateFromTask(this.BeginAddAsync, canAdd);
            this.EditCommand = ReactiveCommand.CreateFromTask(this.BeginEditAsync, canChange);
            this.DeleteCommand = ReactiveCommand.Create(this.BeginDelete, canChange);
            this.ConfirmDeleteCommand = ReactiveCommand.CreateFromTask(this.ConfirmDeleteAsync);
            this.CancelDeleteCommand = ReactiveCommand.Create(this.CancelDelete);
            this.DismissOverlapWarningCommand = ReactiveCommand.Create(() => { this.OverlapWarning = string.Empty; });
            this.DismissActionErrorCommand = ReactiveCommand.Create(() => { this.ActionError = string.Empty; });
        }

        /// <summary>Gets the task of the most recent save or conflict follow-up (close the form, reload); used to await it.</summary>
        public Task PendingOperation { get; private set; } = Task.CompletedTask;

        /// <summary>Gets the command that opens the form for a new manual entry.</summary>
        public ICommand AddCommand { get; }

        /// <summary>Gets the command that opens the form for the selected entry.</summary>
        public ICommand EditCommand { get; }

        /// <summary>Gets the command that asks to delete the selected entry.</summary>
        public ICommand DeleteCommand { get; }

        /// <summary>Gets the command that deletes the entry waiting for confirmation.</summary>
        public ICommand ConfirmDeleteCommand { get; }

        /// <summary>Gets the command that withdraws the delete request.</summary>
        public ICommand CancelDeleteCommand { get; }

        /// <summary>Gets the command that hides the overlap warning.</summary>
        public ICommand DismissOverlapWarningCommand { get; }

        /// <summary>Gets the command that hides the action error.</summary>
        public ICommand DismissActionErrorCommand { get; }

        /// <summary>Gets a value indicating whether entries can be added, edited, and deleted.</summary>
        public bool IsEditingAvailable => this._editingService is not null;

        /// <summary>Gets the open add/edit form, or null.</summary>
        public WorklogEntryEditorViewModel? Editor
        {
            get => this._editor;
            private set
            {
                this.RaiseAndSetIfChanged(ref this._editor, value);
                this.RaisePropertyChanged(nameof(this.IsEditorOpen));
                this.UpdateDialogState();
            }
        }

        /// <summary>Gets a value indicating whether the add/edit form is open.</summary>
        public bool IsEditorOpen => this._editor is not null;

        /// <summary>Gets the entry waiting for delete confirmation, or null.</summary>
        public WorklogEntryRowViewModel? PendingDelete
        {
            get => this._pendingDelete;
            private set
            {
                this.RaiseAndSetIfChanged(ref this._pendingDelete, value);
                this.RaisePropertyChanged(nameof(this.IsDeleteConfirmOpen));
                this.RaisePropertyChanged(nameof(this.DeletePrompt));
                this.UpdateDialogState();
            }
        }

        /// <summary>Gets a value indicating whether the delete confirmation is shown.</summary>
        public bool IsDeleteConfirmOpen => this._pendingDelete is not null;

        /// <summary>Gets the question shown in the delete confirmation.</summary>
        public string DeletePrompt => this._pendingDelete is { } row
            ? $"Delete this entry? {row.Summary}, {row.DurationText}. This cannot be undone."
            : string.Empty;

        /// <summary>Gets a dismissible warning about entries that overlap the one just saved, or an empty string.</summary>
        public string OverlapWarning
        {
            get => this._overlapWarning;
            private set
            {
                this.RaiseAndSetIfChanged(ref this._overlapWarning, value);
                this.RaisePropertyChanged(nameof(this.HasOverlapWarning));
            }
        }

        /// <summary>Gets a value indicating whether the overlap warning is shown.</summary>
        public bool HasOverlapWarning => this._overlapWarning.Length > 0;

        /// <summary>Gets a dismissible message about a delete or an out-of-date entry, or an empty string.</summary>
        public string ActionError
        {
            get => this._actionError;
            private set
            {
                this.RaiseAndSetIfChanged(ref this._actionError, value);
                this.RaisePropertyChanged(nameof(this.HasActionError));
            }
        }

        /// <summary>Gets a value indicating whether the action error is shown.</summary>
        public bool HasActionError => this._actionError.Length > 0;

        /// <summary>Gets the command that reloads the day.</summary>
        public ICommand RefreshCommand { get; }

        /// <summary>Gets the local day that is shown.</summary>
        public DateOnly Day => this._day;

        /// <summary>Gets the entries to display, in start order.</summary>
        public IReadOnlyList<WorklogEntryRowViewModel> Rows
        {
            get => this._rows;
            private set => this.RaiseAndSetIfChanged(ref this._rows, value);
        }

        /// <summary>Gets the entries from the most recent successful load; the Timeline uses the same list.</summary>
        public IReadOnlyList<TimeEntry> LoadedEntries
        {
            get => this._loadedEntries;
            private set => this.RaiseAndSetIfChanged(ref this._loadedEntries, value);
        }

        /// <summary>Gets or sets the selected row.</summary>
        public WorklogEntryRowViewModel? SelectedRow
        {
            get => this._selectedRow;
            set
            {
                this.RaiseAndSetIfChanged(ref this._selectedRow, value);
                this.RaisePropertyChanged(nameof(this.HasSelection));
                this.RaisePropertyChanged(nameof(this.IsDetailsVisible));
            }
        }

        /// <summary>
        /// Gets a value indicating whether the details of the selected row are shown. They are hidden while a form or
        /// confirmation is open, so the table keeps room to stay visible.
        /// </summary>
        public bool IsDetailsVisible => this._selectedRow is not null && !this._isDialogOpen;

        /// <summary>
        /// Gets or sets a value indicating whether an add or edit dialog is open. While it is, the host does not
        /// reload on window activation, so typing is never interrupted.
        /// </summary>
        public bool IsDialogOpen
        {
            get => this._isDialogOpen;
            set
            {
                this.RaiseAndSetIfChanged(ref this._isDialogOpen, value);
                this.RaisePropertyChanged(nameof(this.IsDetailsVisible));
            }
        }

        /// <summary>Gets a value indicating whether a row is selected.</summary>
        public bool HasSelection => this._selectedRow is not null;

        /// <summary>Gets the current load state.</summary>
        public SummaryViewStatus Status
        {
            get => this._status;
            private set
            {
                this.RaiseAndSetIfChanged(ref this._status, value);
                this.RaisePropertyChanged(nameof(this.IsLoading));
                this.RaisePropertyChanged(nameof(this.IsReady));
                this.RaisePropertyChanged(nameof(this.IsMessageVisible));
            }
        }

        /// <summary>Gets the message shown for the no-data and error states.</summary>
        public string StatusMessage
        {
            get => this._statusMessage;
            private set => this.RaiseAndSetIfChanged(ref this._statusMessage, value);
        }

        /// <summary>Gets a note about records that could not be read, or an empty string.</summary>
        public string WarningText
        {
            get => this._warningText;
            private set
            {
                this.RaiseAndSetIfChanged(ref this._warningText, value);
                this.RaisePropertyChanged(nameof(this.HasWarning));
            }
        }

        /// <summary>Gets a value indicating whether a day is being read.</summary>
        public bool IsLoading => this._status == SummaryViewStatus.Loading;

        /// <summary>Gets a value indicating whether rows are shown.</summary>
        public bool IsReady => this._status == SummaryViewStatus.Ready;

        /// <summary>Gets a value indicating whether a status message is shown.</summary>
        public bool IsMessageVisible => this._status is SummaryViewStatus.NoData or SummaryViewStatus.Error;

        /// <summary>Gets a value indicating whether a warning note is shown.</summary>
        public bool HasWarning => this._warningText.Length > 0;

        /// <summary>Sets the day that the next reload shows.</summary>
        /// <param name="day">The local day.</param>
        public void SetDay(DateOnly day)
        {
            if (this._day != day)
            {
                this._day = day;
                this.RaisePropertyChanged(nameof(this.Day));
                this.OverlapWarning = string.Empty;
                this.ActionError = string.Empty;
                this.CloseEditor();
                this.PendingDelete = null;
            }
        }

        /// <summary>Sets the earliest day the add form offers; set by the host from the retention setting.</summary>
        /// <param name="earliestDay">The earliest day retention keeps, or null for no limit.</param>
        public void SetEarliestDay(DateOnly? earliestDay) => this._earliestDay = earliestDay;

        /// <summary>
        /// Reloads the shown day. A newer reload supersedes an older one.
        /// </summary>
        /// <returns>A task that completes when the view has been updated.</returns>
        public async Task RefreshAsync()
        {
            this._cts?.Cancel();
            this._cts?.Dispose();
            var cts = new CancellationTokenSource();
            this._cts = cts;
            var version = ++this._version;
            var day = this._day;
            this.Status = SummaryViewStatus.Loading;

            WorklogReadResult read;
            try
            {
                var range = SummaryRanges.Day(day, this._timeProvider);
                read = await this._store.QueryAsync(new WorklogQuery(range.StartInclusive, range.EndExclusive), cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception)
            {
                if (version == this._version)
                {
                    this.ShowError("The worklog could not be read.");
                }

                return;
            }

            if (version == this._version)
            {
                this.Apply(read);
            }
        }

        private static string DescribeOverlaps(IReadOnlyList<TimeEntry> overlaps)
        {
            if (overlaps.Count == 0)
            {
                return string.Empty;
            }

            var shown = overlaps.Take(3).Select(e => new WorklogEntryRowViewModel(e).Summary).ToList();
            var more = overlaps.Count > 3 ? $" and {overlaps.Count - 3} more" : string.Empty;
            return $"Saved, but it overlaps {(overlaps.Count == 1 ? "another entry" : "other entries")}: {string.Join("; ", shown)}{more}. "
                + "Overlapping time is counted twice in totals.";
        }

        private void UpdateDialogState() => this.IsDialogOpen = this._editor is not null || this._pendingDelete is not null;

        private void CloseEditor()
        {
            if (this._editor is { } editor)
            {
                editor.Saved -= this.OnEditorSaved;
                editor.Stale -= this.OnEditorStale;
                editor.Cancelled -= this.OnEditorCancelled;
                this.Editor = null;
            }
        }

        private async Task BeginAddAsync()
        {
            if (this._editingService is null)
            {
                return;
            }

            var suggestions = await this.LoadProjectSuggestionsAsync();
            this.OpenEditor(WorklogEntryEditorViewModel.ForAdd(
                this._editingService, this._day, this.GetToday(), this._earliestDay, suggestions));
        }

        private async Task BeginEditAsync()
        {
            if (this._editingService is null || this._selectedRow is not { } row)
            {
                return;
            }

            var suggestions = await this.LoadProjectSuggestionsAsync();
            this.OpenEditor(WorklogEntryEditorViewModel.ForEdit(this._editingService, row.Entry, this.GetToday(), suggestions));
        }

        private void OpenEditor(WorklogEntryEditorViewModel editor)
        {
            this.CloseEditor();
            this.ActionError = string.Empty;
            editor.Saved += this.OnEditorSaved;
            editor.Stale += this.OnEditorStale;
            editor.Cancelled += this.OnEditorCancelled;
            this.Editor = editor;
        }

        private DateOnly GetToday() => DateOnly.FromDateTime(this._timeProvider.GetLocalNow().DateTime);

        private void OnEditorCancelled() => this.CloseEditor();

        private void OnEditorSaved(WorklogEditResult result) => this.PendingOperation = this.HandleSavedAsync(result);

        private void OnEditorStale(WorklogEditResult result) => this.PendingOperation = this.HandleStaleAsync(result);

        private Task HandleSavedAsync(WorklogEditResult result)
        {
            this.CloseEditor();
            this.OverlapWarning = DescribeOverlaps(result.OverlappingEntries);
            return this.RefreshAsync();
        }

        private Task HandleStaleAsync(WorklogEditResult result)
        {
            this.CloseEditor();
            this.ActionError = result.Message ?? "The entry changed elsewhere. The day has been reloaded.";
            return this.RefreshAsync();
        }

        private void BeginDelete()
        {
            if (this._selectedRow is { } row)
            {
                this.PendingDelete = row;
            }
        }

        private void CancelDelete() => this.PendingDelete = null;

        private async Task ConfirmDeleteAsync()
        {
            if (this._editingService is null || this._pendingDelete is not { } row)
            {
                return;
            }

            this.PendingDelete = null;
            WorklogEditResult result;
            try
            {
                result = await this._editingService.DeleteAsync(row.Entry);
            }
            catch (Exception)
            {
                this.ActionError = "The entry could not be deleted.";
                return;
            }

            if (!result.IsSuccess)
            {
                this.ActionError = result.Message ?? "The entry could not be deleted.";
            }

            if (result.IsSuccess || result.NeedsReload)
            {
                await this.RefreshAsync();
            }
        }

        private async Task<IReadOnlyList<string>> LoadProjectSuggestionsAsync()
        {
            var projects = this._loadedEntries.Select(e => e.ProjectTag).ToList();
            if (this._day != this.GetToday())
            {
                try
                {
                    var range = SummaryRanges.Today(this._timeProvider);
                    var read = await this._store.QueryAsync(new WorklogQuery(range.StartInclusive, range.EndExclusive));
                    if (read.Outcome.IsSuccess)
                    {
                        projects.AddRange(read.Entries.Select(e => e.ProjectTag));
                    }
                }
                catch (Exception)
                {
                    // Suggestions are a convenience; typing a project still works.
                }
            }

            return projects
                .Select(p => p?.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .Select(p => p!)
                .GroupBy(p => p, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void Apply(WorklogReadResult read)
        {
            if (!read.Outcome.IsSuccess)
            {
                this.ShowError(read.Outcome.Message ?? "The worklog could not be read.");
                return;
            }

            var selectedId = this._selectedRow?.Entry.EntryId;
            var ordered = read.Entries.OrderBy(e => e.StartedAt).ThenBy(e => e.EndedAt).ToList();
            this.LoadedEntries = ordered;
            this.Rows = ordered.Select(e => new WorklogEntryRowViewModel(e)).ToList();
            this.SelectedRow = selectedId is null ? null : this._rows.FirstOrDefault(r => r.Entry.EntryId == selectedId);
            var warnings = read.Outcome.Warnings?.Count ?? 0;
            this.WarningText = warnings == 0
                ? string.Empty
                : $"{warnings} worklog record(s) could not be read and are not shown.";

            if (ordered.Count == 0)
            {
                this.StatusMessage = warnings > 0 ? "No readable entries for this day." : "No entries for this day.";
                this.Status = SummaryViewStatus.NoData;
                return;
            }

            this.StatusMessage = string.Empty;
            this.Status = SummaryViewStatus.Ready;
        }

        private void ShowError(string message)
        {
            this.Rows = [];
            this.LoadedEntries = [];
            this.SelectedRow = null;
            this.WarningText = string.Empty;
            this.StatusMessage = message;
            this.Status = SummaryViewStatus.Error;
        }
    }
}
