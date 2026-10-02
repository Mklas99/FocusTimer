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
        private IReadOnlyList<WorklogEntryRowViewModel> _rows = [];
        private IReadOnlyList<TimeEntry> _loadedEntries = [];
        private WorklogEntryRowViewModel? _selectedRow;
        private SummaryViewStatus _status = SummaryViewStatus.Idle;
        private string _statusMessage = string.Empty;
        private string _warningText = string.Empty;
        private bool _isDialogOpen;
        private DateOnly _day;
        private CancellationTokenSource? _cts;
        private int _version;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorklogEntriesViewModel"/> class.
        /// </summary>
        /// <param name="store">The worklog to read.</param>
        /// <param name="timeProvider">The clock and time zone that decide the local day.</param>
        public WorklogEntriesViewModel(IWorklogStore store, TimeProvider timeProvider)
        {
            this._store = store;
            this._timeProvider = timeProvider;
            this._day = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
            this.RefreshCommand = ReactiveCommand.CreateFromTask(this.RefreshAsync);
        }

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
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether an add or edit dialog is open. While it is, the host does not
        /// reload on window activation, so typing is never interrupted.
        /// </summary>
        public bool IsDialogOpen
        {
            get => this._isDialogOpen;
            set => this.RaiseAndSetIfChanged(ref this._isDialogOpen, value);
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
            }
        }

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
