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
    using FocusTimer.Core.Services;
    using ReactiveUI;

    /// <summary>
    /// Shows a grouped breakdown of tracked time. It has no dependency on the window that hosts it,
    /// so the same view model can be placed in the Settings window or in a future report window.
    /// </summary>
    public class WorklogSummaryViewModel : ReactiveObject
    {
        private readonly IWorklogSummaryService _summaryService;
        private readonly TimeProvider _timeProvider;
        private IReadOnlyList<SummaryRowViewModel> _rows = [];
        private GroupingOption _selectedGrouping;
        private SummaryViewStatus _status = SummaryViewStatus.Idle;
        private string _totalText = string.Empty;
        private string _statusMessage = string.Empty;
        private string _warningText = string.Empty;
        private CancellationTokenSource? _cts;
        private int _version;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorklogSummaryViewModel"/> class.
        /// </summary>
        /// <param name="summaryService">The service that produces summaries.</param>
        /// <param name="groupings">The available groupings.</param>
        /// <param name="timeProvider">The clock used to resolve the range.</param>
        public WorklogSummaryViewModel(
            IWorklogSummaryService summaryService,
            WorklogGroupingRegistry groupings,
            TimeProvider timeProvider)
        {
            this._summaryService = summaryService;
            this._timeProvider = timeProvider;
            this.Groupings = groupings.All.Select(g => new GroupingOption(g.Id, g.DisplayName)).ToList();
            this._selectedGrouping = this.Groupings.FirstOrDefault() ?? new GroupingOption(string.Empty, string.Empty);
            this.RangeSelector = () => SummaryRanges.Today(this._timeProvider);
            this.RangeLabel = "Today";
            this.RefreshCommand = ReactiveCommand.CreateFromTask(this.RefreshAsync);
        }

        /// <summary>Gets the groupings the user can choose from.</summary>
        public IReadOnlyList<GroupingOption> Groupings { get; }

        /// <summary>Gets the command that reloads the summary.</summary>
        public ICommand RefreshCommand { get; }

        /// <summary>Gets or sets the text describing the current range, such as "Today".</summary>
        public string RangeLabel { get; set; }

        /// <summary>Gets or sets what decides the range on each refresh. Defaults to the current local day.</summary>
        public Func<SummaryRange> RangeSelector { get; set; }

        /// <summary>Gets or sets the filter applied on each refresh. Defaults to no filter.</summary>
        public SummaryFilter Filter { get; set; } = SummaryFilter.None;

        /// <summary>Gets the task of the most recent refresh started by a grouping change; used to await it.</summary>
        public Task PendingRefresh { get; private set; } = Task.CompletedTask;

        /// <summary>Gets or sets the chosen grouping. Changing it reloads the summary.</summary>
        public GroupingOption SelectedGrouping
        {
            get => this._selectedGrouping;
            set
            {
                if (value is null || Equals(this._selectedGrouping, value))
                {
                    return;
                }

                this.RaiseAndSetIfChanged(ref this._selectedGrouping, value);
                this.PendingRefresh = this.RefreshAsync();
            }
        }

        /// <summary>Gets the rows to display.</summary>
        public IReadOnlyList<SummaryRowViewModel> Rows
        {
            get => this._rows;
            private set => this.RaiseAndSetIfChanged(ref this._rows, value);
        }

        /// <summary>Gets the formatted total, or an empty string when there is none to show.</summary>
        public string TotalText
        {
            get => this._totalText;
            private set => this.RaiseAndSetIfChanged(ref this._totalText, value);
        }

        /// <summary>Gets the current state.</summary>
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

        /// <summary>Gets a value indicating whether a summary is being read.</summary>
        public bool IsLoading => this._status == SummaryViewStatus.Loading;

        /// <summary>Gets a value indicating whether rows are shown.</summary>
        public bool IsReady => this._status == SummaryViewStatus.Ready;

        /// <summary>Gets a value indicating whether a status message is shown.</summary>
        public bool IsMessageVisible => this._status is SummaryViewStatus.NoData or SummaryViewStatus.Error;

        /// <summary>Gets a value indicating whether a warning note is shown.</summary>
        public bool HasWarning => this._warningText.Length > 0;

        /// <summary>
        /// Reloads the summary for the current range, grouping, and filter. A newer refresh supersedes an older one.
        /// </summary>
        /// <returns>A task that completes when the view has been updated.</returns>
        public async Task RefreshAsync()
        {
            this._cts?.Cancel();
            this._cts?.Dispose();
            var cts = new CancellationTokenSource();
            this._cts = cts;
            var version = ++this._version;
            this.Status = SummaryViewStatus.Loading;

            WorklogSummary summary;
            try
            {
                var request = new WorklogSummaryRequest(this.RangeSelector(), this._selectedGrouping.Id, this.Filter);
                summary = await this._summaryService.SummarizeAsync(request, cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception)
            {
                if (version == this._version)
                {
                    this.ShowError("The worklog could not be summarized.");
                }

                return;
            }

            if (version == this._version)
            {
                this.Apply(summary);
            }
        }

        private void Apply(WorklogSummary summary)
        {
            if (!summary.IsSuccess)
            {
                this.ShowError(summary.Outcome.Message ?? "The worklog could not be read.");
                return;
            }

            this.Rows = summary.Rows.Select(r => new SummaryRowViewModel(r)).ToList();
            this.WarningText = summary.Warnings.Count == 0
                ? string.Empty
                : $"{summary.Warnings.Count} worklog record(s) could not be read and are not included.";

            if (summary.Rows.Count == 0)
            {
                this.TotalText = string.Empty;
                this.StatusMessage = $"No time tracked {this.RangeLabel.ToLowerInvariant()}.";
                this.Status = SummaryViewStatus.NoData;
                return;
            }

            this.TotalText = SummaryFormatting.Duration(summary.Total);
            this.StatusMessage = string.Empty;
            this.Status = SummaryViewStatus.Ready;
        }

        private void ShowError(string message)
        {
            this.Rows = [];
            this.TotalText = string.Empty;
            this.WarningText = string.Empty;
            this.StatusMessage = message;
            this.Status = SummaryViewStatus.Error;
        }
    }
}
