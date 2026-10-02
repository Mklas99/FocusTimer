namespace FocusTimer.App.ViewModels
{
    using System.Globalization;
    using FocusTimer.Core.Models;

    /// <summary>
    /// One stored worklog entry as shown in the Entries table. The wrapped entry carries the
    /// revision the user saw, so an edit or delete can detect that it changed meanwhile.
    /// </summary>
    public sealed class WorklogEntryRowViewModel
    {
        /// <summary>The text shown for an empty project or window.</summary>
        public const string EmptyValueText = "—";

        /// <summary>
        /// Initializes a new instance of the <see cref="WorklogEntryRowViewModel"/> class.
        /// </summary>
        /// <param name="entry">The stored entry.</param>
        public WorklogEntryRowViewModel(TimeEntry entry)
        {
            this.Entry = entry;
            this.StartText = entry.StartedAt.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            this.EndText = entry.EndedAt.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            this.DurationText = FormatDuration(entry.Duration);
            this.ApplicationText = entry.AppName;
            this.WindowText = string.IsNullOrEmpty(entry.WindowTitle) ? EmptyValueText : entry.WindowTitle;
            this.ProjectText = string.IsNullOrWhiteSpace(entry.ProjectTag) ? EmptyValueText : entry.ProjectTag;
            this.IsManual = entry.CaptureSource == CaptureSource.Manual;
            this.SourceText = this.IsManual ? "Manual" : "Tracked";
        }

        /// <summary>Gets the stored entry.</summary>
        public TimeEntry Entry { get; }

        /// <summary>Gets the start time of day.</summary>
        public string StartText { get; }

        /// <summary>Gets the end time of day.</summary>
        public string EndText { get; }

        /// <summary>Gets the duration as hours, minutes, and seconds.</summary>
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
        public string ProjectSourceText => WorklogValueCodec.ToStoredValue(this.Entry.ProjectAssignmentSource);

        /// <summary>Gets the platform the entry came from.</summary>
        public string PlatformText => WorklogValueCodec.ToStoredValue(this.Entry.SourcePlatform);

        /// <summary>Gets the device ID, or a dash when none is stored.</summary>
        public string DeviceText => string.IsNullOrEmpty(this.Entry.SourceDeviceId) ? EmptyValueText : this.Entry.SourceDeviceId;

        /// <summary>Gets a one-line description used in warnings and confirmations.</summary>
        public string Summary => $"{this.StartText}–{this.EndText} {this.ApplicationText}";

        private static string FormatDuration(System.TimeSpan duration) =>
            string.Create(CultureInfo.InvariantCulture, $"{(int)duration.TotalHours}:{duration.Minutes:D2}:{duration.Seconds:D2}");
    }
}
