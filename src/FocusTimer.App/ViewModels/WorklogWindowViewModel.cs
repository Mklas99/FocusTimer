namespace FocusTimer.App.ViewModels
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;
    using FocusTimer.Core.Services;
    using ReactiveUI;

    /// <summary>The tabs of the Worklog window.</summary>
    public enum WorklogTab
    {
        /// <summary>The table of stored entries.</summary>
        Entries,

        /// <summary>The chronological view of the day.</summary>
        Timeline,

        /// <summary>The grouped totals.</summary>
        Summary,
    }

    /// <summary>
    /// Owns the day shown by the Worklog window and pushes it to every tab. It does not depend on the
    /// Settings window or its draft.
    /// </summary>
    public class WorklogWindowViewModel : ReactiveObject
    {
        private readonly ISettingsProvider _settingsProvider;
        private readonly TimeProvider _timeProvider;
        private DateOnly _selectedDay;
        private DateOnly? _earliestDay;
        private WorklogTab _selectedTab = WorklogTab.Entries;
        private string _dayMessage = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorklogWindowViewModel"/> class.
        /// </summary>
        /// <param name="entries">The Entries tab.</param>
        /// <param name="summary">The Summary tab.</param>
        /// <param name="settingsProvider">Supplies the retention setting that limits selectable days.</param>
        /// <param name="timeProvider">The clock and time zone that decide the local day.</param>
        public WorklogWindowViewModel(
            WorklogEntriesViewModel entries,
            WorklogSummaryViewModel summary,
            ISettingsProvider settingsProvider,
            TimeProvider timeProvider)
        {
            this.Entries = entries;
            this.Timeline = new WorklogTimelineViewModel(entries);
            this.Summary = summary;
            this._settingsProvider = settingsProvider;
            this._timeProvider = timeProvider;
            this._selectedDay = this.Today;
            this.ApplyDayToTabs();
            this.PreviousDayCommand = ReactiveCommand.CreateFromTask(() => this.SelectDayAsync(this._selectedDay.AddDays(-1)));
            this.NextDayCommand = ReactiveCommand.CreateFromTask(() => this.SelectDayAsync(this._selectedDay.AddDays(1)));
            this.TodayCommand = ReactiveCommand.CreateFromTask(() => this.SelectDayAsync(this.Today));
            this.RefreshCommand = ReactiveCommand.CreateFromTask(this.RefreshAsync);
        }

        /// <summary>Gets the Entries tab.</summary>
        public WorklogEntriesViewModel Entries { get; }

        /// <summary>Gets the Timeline tab; it draws the same loaded day as the Entries tab.</summary>
        public WorklogTimelineViewModel Timeline { get; }

        /// <summary>Gets the Summary tab.</summary>
        public WorklogSummaryViewModel Summary { get; }

        /// <summary>Gets the command that selects the day before the selected one.</summary>
        public ICommand PreviousDayCommand { get; }

        /// <summary>Gets the command that selects the day after the selected one.</summary>
        public ICommand NextDayCommand { get; }

        /// <summary>Gets the command that selects the current local day.</summary>
        public ICommand TodayCommand { get; }

        /// <summary>Gets the command that reloads the selected tab.</summary>
        public ICommand RefreshCommand { get; }

        /// <summary>Gets the current local day.</summary>
        public DateOnly Today => DateOnly.FromDateTime(this._timeProvider.GetLocalNow().DateTime);

        /// <summary>Gets the local day all tabs show.</summary>
        public DateOnly SelectedDay => this._selectedDay;

        /// <summary>Gets the earliest selectable day, or null when retention is off.</summary>
        public DateOnly? EarliestDay
        {
            get => this._earliestDay;
            private set
            {
                this.RaiseAndSetIfChanged(ref this._earliestDay, value);
                this.RaisePropertyChanged(nameof(this.DisplayDateStart));
                this.RaisePropertyChanged(nameof(this.DisplayDateEnd));
            }
        }

        /// <summary>Gets the first date the date picker offers, or null for no limit.</summary>
        public DateTime? DisplayDateStart => this._earliestDay?.ToDateTime(TimeOnly.MinValue);

        /// <summary>Gets the last date the date picker offers: today.</summary>
        public DateTime DisplayDateEnd => this.Today.ToDateTime(TimeOnly.MinValue);

        /// <summary>Gets or sets the selected day for the date picker. An unavailable day is refused.</summary>
        public DateTime? SelectedDate
        {
            get => this._selectedDay.ToDateTime(TimeOnly.MinValue);
            set
            {
                if (value is { } date)
                {
                    this.PendingDaySelection = this.SelectDayAsync(DateOnly.FromDateTime(date));
                }
            }
        }

        /// <summary>Gets the task of the most recent selection made through the date picker; used to await it.</summary>
        public Task PendingDaySelection { get; private set; } = Task.CompletedTask;

        /// <summary>Gets the heading for the selected day.</summary>
        public string SelectedDayText => this._selectedDay == this.Today
            ? "Today"
            : this._selectedDay.ToString("dddd, d MMMM yyyy", CultureInfo.InvariantCulture);

        /// <summary>Gets a note shown when a day was refused or when retention limits the range, or an empty string.</summary>
        public string DayMessage
        {
            get => this._dayMessage;
            private set
            {
                this.RaiseAndSetIfChanged(ref this._dayMessage, value);
                this.RaisePropertyChanged(nameof(this.HasDayMessage));
            }
        }

        /// <summary>Gets a value indicating whether a day note is shown.</summary>
        public bool HasDayMessage => this._dayMessage.Length > 0;

        /// <summary>Gets a value indicating whether the previous day can be selected.</summary>
        public bool CanSelectPrevious => this._earliestDay is not { } earliest || this._selectedDay > earliest;

        /// <summary>Gets a value indicating whether the next day can be selected.</summary>
        public bool CanSelectNext => this._selectedDay < this.Today;

        /// <summary>Gets the selected tab.</summary>
        public WorklogTab SelectedTab => this._selectedTab;

        /// <summary>
        /// Prepares the window for being shown: Entries tab, today, current retention limits, fresh data.
        /// </summary>
        /// <returns>A task that completes when the visible tab has loaded.</returns>
        public async Task OpenAsync()
        {
            this._selectedTab = WorklogTab.Entries;
            this.RaisePropertyChanged(nameof(this.SelectedTab));
            await this.LoadBoundsAsync();
            this.SetDay(this.Today);
            await this.ReloadSelectedTabAsync();
        }

        /// <summary>
        /// Reloads after the user returned to the window, unless an add or edit dialog is open.
        /// </summary>
        /// <returns>A task that completes when the visible tab has loaded, or immediately when skipped.</returns>
        public async Task ActivateAsync()
        {
            if (this.Entries.IsDialogOpen)
            {
                return;
            }

            await this.LoadBoundsAsync();
            this.RaiseDayStateChanged();
            await this.ReloadSelectedTabAsync();
        }

        /// <summary>Handles the user choosing a tab and loads it for the selected day.</summary>
        /// <param name="tab">The chosen tab.</param>
        /// <returns>A task that completes when the tab has loaded.</returns>
        public async Task SelectTabAsync(WorklogTab tab)
        {
            this._selectedTab = tab;
            this.RaisePropertyChanged(nameof(this.SelectedTab));
            await this.ReloadSelectedTabAsync();
        }

        /// <summary>Reloads the selected tab for the selected day.</summary>
        /// <returns>A task that completes when the tab has loaded.</returns>
        public async Task RefreshAsync()
        {
            await this.LoadBoundsAsync();
            this.RaiseDayStateChanged();
            await this.ReloadSelectedTabAsync();
        }

        /// <summary>
        /// Selects a day for every tab. A day after today, or before the retention window, is refused.
        /// </summary>
        /// <param name="day">The requested local day.</param>
        /// <returns>A task that completes when the visible tab has loaded the new day.</returns>
        public async Task SelectDayAsync(DateOnly day)
        {
            await this.LoadBoundsAsync();
            if (day > this.Today)
            {
                this.RaisePropertyChanged(nameof(this.SelectedDate));
                return;
            }

            if (this._earliestDay is { } earliest && day < earliest)
            {
                this.DayMessage = $"The earliest day available is {earliest:yyyy-MM-dd} (data retention).";
                this.RaisePropertyChanged(nameof(this.SelectedDate));
                return;
            }

            if (day == this._selectedDay)
            {
                return;
            }

            this.SetDay(day);
            await this.ReloadSelectedTabAsync();
        }

        private void SetDay(DateOnly day)
        {
            this._selectedDay = day;
            this.ApplyDayToTabs();
            this.RaiseDayStateChanged();
        }

        private void ApplyDayToTabs()
        {
            var day = this._selectedDay;
            var isToday = day == this.Today;
            this.Entries.SetDay(day);
            this.Summary.RangeSelector = () => SummaryRanges.Day(day, this._timeProvider);
            this.Summary.RangeLabel = isToday ? "Today" : day.ToString("ddd, d MMM yyyy", CultureInfo.InvariantCulture);
            this.Summary.RangePhrase = isToday ? "today" : "on " + day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private void RaiseDayStateChanged()
        {
            this.RaisePropertyChanged(nameof(this.SelectedDay));
            this.RaisePropertyChanged(nameof(this.SelectedDate));
            this.RaisePropertyChanged(nameof(this.SelectedDayText));
            this.RaisePropertyChanged(nameof(this.CanSelectPrevious));
            this.RaisePropertyChanged(nameof(this.CanSelectNext));
            this.RaisePropertyChanged(nameof(this.DisplayDateEnd));
        }

        private async Task LoadBoundsAsync()
        {
            int retention;
            try
            {
                retention = (await this._settingsProvider.LoadAsync()).DataRetentionDays;
            }
            catch (Exception)
            {
                retention = 0;
            }

            this.EarliestDay = WorklogDayBounds.EarliestDay(this.Today, retention);
            this.Entries.SetEarliestDay(this._earliestDay);
            this.DayMessage = string.Empty;
            if (this._earliestDay is { } limit && this._selectedDay < limit)
            {
                this.SetDay(limit);
            }
        }

        private Task ReloadSelectedTabAsync() => this._selectedTab switch
        {
            WorklogTab.Summary => this.Summary.RefreshAsync(),
            _ => this.Entries.RefreshAsync(),
        };
    }
}
