namespace FocusTimer.App.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;
    using FocusTimer.Core.Services;
    using ReactiveUI;

    /// <summary>
    /// The form for adding a manual entry or editing an existing one. It talks only to the editing service,
    /// so the host decides where the form is shown.
    /// </summary>
    public sealed class WorklogEntryEditorViewModel : ReactiveObject
    {
        private static readonly string[] TimeFormats = ["H:mm", "HH:mm", "H:mm:ss", "HH:mm:ss"];

        private readonly IWorklogEditingService _service;
        private readonly TimeEntry? _entry;
        private readonly string _initialDurationText;
        private DateTime? _date;
        private string _startText;
        private string _durationText;
        private string _windowText;
        private string _projectText;
        private string _errorText = string.Empty;
        private bool _isSaving;

        private WorklogEntryEditorViewModel(
            IWorklogEditingService service,
            TimeEntry? entry,
            DateOnly day,
            string startText,
            string durationText,
            IReadOnlyList<string> projectSuggestions,
            DateOnly? earliestDay,
            DateOnly today)
        {
            this._service = service;
            this._entry = entry;
            this.Day = day;
            this._date = day.ToDateTime(TimeOnly.MinValue);
            this._startText = startText;
            this._durationText = durationText;
            this._initialDurationText = durationText;
            this._windowText = entry?.WindowTitle ?? string.Empty;
            this._projectText = entry?.ProjectTag ?? string.Empty;
            this.ProjectSuggestions = projectSuggestions;
            this.DisplayDateStart = earliestDay?.ToDateTime(TimeOnly.MinValue);
            this.DisplayDateEnd = today.ToDateTime(TimeOnly.MinValue);
            this.SaveCommand = ReactiveCommand.CreateFromTask(this.SaveAsync);
            this.CancelCommand = ReactiveCommand.Create(() => this.Cancelled?.Invoke());
        }

        /// <summary>Raised when the entry was stored; carries the service result.</summary>
        public event Action<WorklogEditResult>? Saved;

        /// <summary>Raised when the service reported that the entry changed or vanished meanwhile; carries the result.</summary>
        public event Action<WorklogEditResult>? Stale;

        /// <summary>Raised when the user closes the form without saving.</summary>
        public event Action? Cancelled;

        /// <summary>Gets the command that stores the entry.</summary>
        public ICommand SaveCommand { get; }

        /// <summary>Gets the command that closes the form without saving.</summary>
        public ICommand CancelCommand { get; }

        /// <summary>Gets a value indicating whether an existing entry is edited.</summary>
        public bool IsEdit => this._entry is not null;

        /// <summary>Gets the form heading.</summary>
        public string Title => this.IsEdit ? "Edit entry" : "Add manual entry";

        /// <summary>Gets the local day the entry belongs to.</summary>
        public DateOnly Day { get; }

        /// <summary>Gets the application: fixed, shown read-only.</summary>
        public string ApplicationText => this._entry?.AppName ?? WorklogEditingService.ManualApplicationName;

        /// <summary>Gets a value indicating whether the date and start time can be changed (only when adding).</summary>
        public bool IsStartEditable => !this.IsEdit;

        /// <summary>Gets a value indicating whether the start is shown read-only (only when editing).</summary>
        public bool IsStartReadOnly => this.IsEdit;

        /// <summary>Gets the first date the date field offers, or null for no limit.</summary>
        public DateTime? DisplayDateStart { get; }

        /// <summary>Gets the last date the date field offers: today.</summary>
        public DateTime DisplayDateEnd { get; }

        /// <summary>Gets the projects already used on the selected day and today.</summary>
        public IReadOnlyList<string> ProjectSuggestions { get; }

        /// <summary>Gets or sets the chosen date when adding.</summary>
        public DateTime? Date
        {
            get => this._date;
            set => this.RaiseAndSetIfChanged(ref this._date, value);
        }

        /// <summary>Gets or sets the start time text, such as 14:30.</summary>
        public string StartText
        {
            get => this._startText;
            set => this.RaiseAndSetIfChanged(ref this._startText, value);
        }

        /// <summary>Gets or sets the duration text, such as 2h 30m or 2.5h.</summary>
        public string DurationText
        {
            get => this._durationText;
            set => this.RaiseAndSetIfChanged(ref this._durationText, value);
        }

        /// <summary>Gets or sets the optional window text.</summary>
        public string WindowText
        {
            get => this._windowText;
            set => this.RaiseAndSetIfChanged(ref this._windowText, value);
        }

        /// <summary>Gets or sets the project text; typed text and dropdown choices both end up here.</summary>
        public string ProjectText
        {
            get => this._projectText;
            set => this.RaiseAndSetIfChanged(ref this._projectText, value ?? string.Empty);
        }

        /// <summary>Gets the message about the last save attempt, or an empty string.</summary>
        public string ErrorText
        {
            get => this._errorText;
            private set
            {
                this.RaiseAndSetIfChanged(ref this._errorText, value);
                this.RaisePropertyChanged(nameof(this.HasError));
            }
        }

        /// <summary>Gets a value indicating whether a message is shown.</summary>
        public bool HasError => this._errorText.Length > 0;

        /// <summary>Gets a value indicating whether a save is running.</summary>
        public bool IsSaving
        {
            get => this._isSaving;
            private set => this.RaiseAndSetIfChanged(ref this._isSaving, value);
        }

        /// <summary>Creates the form for a new manual entry.</summary>
        /// <param name="service">The editing service.</param>
        /// <param name="day">The day to start with.</param>
        /// <param name="today">The current local day.</param>
        /// <param name="earliestDay">The earliest day retention keeps, or null.</param>
        /// <param name="suggestions">The projects to offer.</param>
        /// <returns>The form.</returns>
        public static WorklogEntryEditorViewModel ForAdd(
            IWorklogEditingService service,
            DateOnly day,
            DateOnly today,
            DateOnly? earliestDay,
            IReadOnlyList<string> suggestions) =>
            new(service, null, day, string.Empty, string.Empty, suggestions, earliestDay, today);

        /// <summary>Creates the form for an existing entry.</summary>
        /// <param name="service">The editing service.</param>
        /// <param name="entry">The entry as loaded; its revision is sent with the change.</param>
        /// <param name="today">The current local day.</param>
        /// <param name="suggestions">The projects to offer.</param>
        /// <returns>The form.</returns>
        public static WorklogEntryEditorViewModel ForEdit(
            IWorklogEditingService service,
            TimeEntry entry,
            DateOnly today,
            IReadOnlyList<string> suggestions) =>
            new(
                service,
                entry,
                DateOnly.FromDateTime(entry.StartedAt.Date),
                entry.StartedAt.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                DurationParser.Format(entry.Duration),
                suggestions,
                null,
                today);

        /// <summary>Validates the form and stores the entry.</summary>
        /// <returns>A task that completes when the attempt is finished.</returns>
        public async Task SaveAsync()
        {
            if (this._isSaving)
            {
                return;
            }

            this.ErrorText = string.Empty;
            if (!DurationParser.TryParse(this._durationText, out var duration, out var durationError))
            {
                this.ErrorText = durationError;
                return;
            }

            DateOnly day = this.Day;
            TimeOnly start = default;
            if (!this.IsEdit)
            {
                if (this._date is not { } date)
                {
                    this.ErrorText = "Choose a date.";
                    return;
                }

                if (!TimeOnly.TryParseExact((this._startText ?? string.Empty).Trim(), TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out start))
                {
                    this.ErrorText = "Enter the start time like 14:30.";
                    return;
                }

                day = DateOnly.FromDateTime(date);
            }

            this.IsSaving = true;
            try
            {
                WorklogEditResult result;
                if (this._entry is { } entry)
                {
                    // An untouched duration is sent back exactly as stored, so seconds are never rounded away.
                    var keepDuration = string.Equals(this._durationText.Trim(), this._initialDurationText, StringComparison.Ordinal);
                    result = await this._service.UpdateAsync(
                        entry,
                        new WorklogEntryEdit(this._windowText, this._projectText, keepDuration ? entry.Duration : duration));
                }
                else
                {
                    result = await this._service.AddManualAsync(new ManualEntryRequest(day, start, duration, this._windowText, this._projectText));
                }

                if (result.IsSuccess)
                {
                    this.Saved?.Invoke(result);
                }
                else if (result.NeedsReload)
                {
                    this.Stale?.Invoke(result);
                }
                else
                {
                    this.ErrorText = result.Message ?? "The entry could not be saved.";
                }
            }
            catch (Exception)
            {
                this.ErrorText = "The entry could not be saved.";
            }
            finally
            {
                this.IsSaving = false;
            }
        }
    }
}
