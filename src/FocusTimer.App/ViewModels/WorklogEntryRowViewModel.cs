namespace FocusTimer.App.ViewModels
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using FocusTimer.Core.Models;
    using ReactiveUI;

    /// <summary>
    /// One stored worklog entry as shown in the Entries table. The wrapped entry carries the
    /// revision the user saw, so an edit or delete can detect that it changed meanwhile.
    /// </summary>
    public sealed class WorklogEntryRowViewModel : ReactiveObject
    {
        /// <summary>The text shown for an empty project or window.</summary>
        public const string EmptyValueText = "—";

        private readonly string _projectForSearch;
        private IReadOnlyList<string> _highlightTerms;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorklogEntryRowViewModel"/> class.
        /// </summary>
        /// <param name="entry">The stored entry.</param>
        /// <param name="resolvedProject">The project after applying project rules; the stored project when null.</param>
        public WorklogEntryRowViewModel(TimeEntry entry, string? resolvedProject = null)
        {
            string? project = string.IsNullOrWhiteSpace(resolvedProject) ? entry.ProjectTag : resolvedProject;
            this._projectForSearch = project ?? string.Empty;
            this._highlightTerms = [];
            this.Entry = entry;
            this.StartText = entry.StartedAt.ToString("HH:mm", CultureInfo.InvariantCulture);
            this.EndText = entry.EndedAt.ToString("HH:mm", CultureInfo.InvariantCulture);
            this.DurationText = FormatDuration(entry.Duration);
            this.ExactTimeText = string.Create(
                CultureInfo.InvariantCulture,
                $"{entry.StartedAt:HH:mm:ss}\u2013{entry.EndedAt:HH:mm:ss} ({FormatExactDuration(entry.Duration)})");
            this.ApplicationText = entry.AppName;
            this.WindowText = string.IsNullOrEmpty(entry.WindowTitle) ? EmptyValueText : entry.WindowTitle;
            this.ProjectText = string.IsNullOrWhiteSpace(project) ? EmptyValueText : project;
            this.ProjectFromRule = string.IsNullOrWhiteSpace(entry.ProjectTag) && !string.IsNullOrWhiteSpace(project);
            this.IsManual = entry.CaptureSource == CaptureSource.Manual;
            this.SourceText = this.IsManual ? "Manual" : "Tracked";
        }

        /// <summary>Gets the stored entry.</summary>
        public TimeEntry Entry { get; }

        /// <summary>
        /// Gets or sets the search words to highlight in the table's text cells; empty when no search is active.
        /// </summary>
        public IReadOnlyList<string> HighlightTerms
        {
            get => this._highlightTerms;
            set => this.RaiseAndSetIfChanged(ref this._highlightTerms, value);
        }

        /// <summary>Gets the start, end, and duration down to the second, shown in the details of the selected row.</summary>
        public string ExactTimeText { get; }

        /// <summary>Gets the start time of day, to the minute.</summary>
        public string StartText { get; }

        /// <summary>Gets the end time of day, to the minute.</summary>
        public string EndText { get; }

        /// <summary>Gets the duration as hours and minutes, or "&lt;1m" under a minute.</summary>
        public string DurationText { get; }

        /// <summary>Gets the application name.</summary>
        public string ApplicationText { get; }

        /// <summary>Gets the window title, or a dash when empty.</summary>
        public string WindowText { get; }

        /// <summary>Gets the project, or a dash when empty.</summary>
        public string ProjectText { get; }

        /// <summary>Gets the capture source as text: Tracked or Manual.</summary>
        public string SourceText { get; }

        /// <summary>Gets a value indicating whether the user added this entry by hand.</summary>
        public bool IsManual { get; }

        /// <summary>Gets the stored entry ID.</summary>
        public string EntryIdText => this.Entry.EntryId;

        /// <summary>Gets the session ID.</summary>
        public string SessionIdText => this.Entry.SessionId;

        /// <summary>Gets the stored revision.</summary>
        public string RevisionText => this.Entry.Revision.ToString(CultureInfo.InvariantCulture);

        /// <summary>Gets the last-modified time in UTC.</summary>
        public string LastModifiedText => this.Entry.LastModifiedAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);

        /// <summary>Gets how the entry ended.</summary>
        public string EndReasonText => WorklogValueCodec.ToStoredValue(this.Entry.EndReason);

        /// <summary>Gets how the project was assigned.</summary>
        public string ProjectSourceText => this.ProjectFromRule
            ? "rule"
            : WorklogValueCodec.ToStoredValue(this.Entry.ProjectAssignmentSource);

        /// <summary>Gets a value indicating whether the shown project comes from a project rule, not from the stored entry.</summary>
        public bool ProjectFromRule { get; }

        /// <summary>Gets the platform the entry came from.</summary>
        public string PlatformText => WorklogValueCodec.ToStoredValue(this.Entry.SourcePlatform);

        /// <summary>Gets the device ID, or a dash when none is stored.</summary>
        public string DeviceText => string.IsNullOrEmpty(this.Entry.SourceDeviceId) ? EmptyValueText : this.Entry.SourceDeviceId;

        /// <summary>Gets a one-line description used in warnings and confirmations.</summary>
        public string Summary => $"{this.StartText}–{this.EndText} {this.ApplicationText}";

        /// <summary>
        /// Tells whether the row matches every search term (a case-insensitive part of its application, window,
        /// project, source, or times). No terms match everything.
        /// </summary>
        /// <param name="terms">The search terms; all must match.</param>
        /// <returns>True when every term is found.</returns>
        public bool Matches(IReadOnlyList<string> terms)
        {
            if (terms.Count == 0)
            {
                return true;
            }

            var haystack = string.Join(
                '\n',
                this.Entry.AppName,
                this.Entry.WindowTitle,
                this._projectForSearch,
                this.SourceText,
                this.StartText,
                this.EndText,
                this.DurationText);
            return terms.All(term => haystack.Contains(term, System.StringComparison.OrdinalIgnoreCase));
        }

        // Rounded to the nearest minute (not cut off), so it agrees with the start and end shown to the minute.
        private static string FormatDuration(System.TimeSpan duration) =>
            SummaryFormatting.Duration(duration < System.TimeSpan.FromMinutes(1)
                ? duration
                : System.TimeSpan.FromMinutes(System.Math.Round(duration.TotalMinutes, System.MidpointRounding.AwayFromZero)));

        private static string FormatExactDuration(System.TimeSpan duration) =>
            string.Create(CultureInfo.InvariantCulture, $"{(int)duration.TotalHours}:{duration.Minutes:D2}:{duration.Seconds:D2}");
    }
}
