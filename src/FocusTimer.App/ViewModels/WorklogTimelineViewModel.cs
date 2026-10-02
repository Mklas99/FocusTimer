namespace FocusTimer.App.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using FocusTimer.Core.Models;
    using ReactiveUI;

    /// <summary>
    /// Shows the day that the Entries tab loaded as blocks along a time axis. It reads the same loaded list as the
    /// table, so the two can never disagree.
    /// </summary>
    public class WorklogTimelineViewModel : ReactiveObject
    {
        /// <summary>Blocks shorter than this many minutes are laid out as if they were this long, so they stay visible.</summary>
        public const double MinimumDisplayMinutes = 18;

        private readonly WorklogEntriesViewModel _entries;
        private IReadOnlyList<TimelineBlockViewModel> _blocks = [];
        private double _scrollTargetMinute;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorklogTimelineViewModel"/> class.
        /// </summary>
        /// <param name="entries">The Entries tab whose loaded day is drawn.</param>
        public WorklogTimelineViewModel(WorklogEntriesViewModel entries)
        {
            this._entries = entries;
            this._entries.PropertyChanged += this.OnEntriesChanged;
            this.Rebuild();
        }

        /// <summary>Gets the Entries tab; the view binds its load state and read warnings.</summary>
        public WorklogEntriesViewModel Entries => this._entries;

        /// <summary>Gets the blocks, in start order.</summary>
        public IReadOnlyList<TimelineBlockViewModel> Blocks
        {
            get => this._blocks;
            private set
            {
                this.RaiseAndSetIfChanged(ref this._blocks, value);
                this.RaisePropertyChanged(nameof(this.HasBlocks));
            }
        }

        /// <summary>Gets the labels of the 24 hour rows of the axis, such as 09:00.</summary>
        public IReadOnlyList<string> HourLabels { get; } = Enumerable.Range(0, 24).Select(h => $"{h:D2}:00").ToList();

        /// <summary>Gets a value indicating whether there is anything to draw.</summary>
        public bool HasBlocks => this._blocks.Count > 0;

        /// <summary>Gets the minute of day the view scrolls to: half an hour before the first entry.</summary>
        public double ScrollTargetMinute
        {
            get => this._scrollTargetMinute;
            private set => this.RaiseAndSetIfChanged(ref this._scrollTargetMinute, value);
        }

        /// <summary>
        /// Places entries into lanes: entries that overlap in time (at their displayed length) are drawn side by side.
        /// </summary>
        /// <param name="rows">The rows to place.</param>
        /// <returns>The blocks in start order.</returns>
        public static IReadOnlyList<TimelineBlockViewModel> Layout(IReadOnlyList<WorklogEntryRowViewModel> rows)
        {
            var ordered = rows.OrderBy(r => r.Entry.StartedAt).ThenBy(r => r.Entry.EndedAt).ToList();
            var placed = new List<(WorklogEntryRowViewModel Row, int Lane, int Group)>();
            var groupLaneCounts = new List<int>();
            var laneEnds = new List<double>();
            var groupEnd = double.MinValue;
            var group = -1;
            foreach (var row in ordered)
            {
                var start = row.Entry.StartedAt.TimeOfDay.TotalMinutes;
                var end = start + Math.Max(row.Entry.Duration.TotalMinutes, MinimumDisplayMinutes);
                if (start >= groupEnd)
                {
                    group++;
                    groupLaneCounts.Add(0);
                    laneEnds.Clear();
                    groupEnd = double.MinValue;
                }

                var lane = laneEnds.FindIndex(laneEnd => laneEnd <= start);
                if (lane < 0)
                {
                    laneEnds.Add(end);
                    lane = laneEnds.Count - 1;
                }
                else
                {
                    laneEnds[lane] = end;
                }

                groupLaneCounts[group] = laneEnds.Count;
                groupEnd = Math.Max(groupEnd, end);
                placed.Add((row, lane, group));
            }

            return placed.Select(p => new TimelineBlockViewModel(p.Row, p.Lane, groupLaneCounts[p.Group])).ToList();
        }

        private void OnEntriesChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WorklogEntriesViewModel.Rows))
            {
                this.Rebuild();
            }
        }

        private void Rebuild()
        {
            this.Blocks = Layout(this._entries.Rows);
            this.ScrollTargetMinute = this._blocks.Count == 0
                ? 8 * 60
                : Math.Max(0, this._blocks.Min(b => b.StartMinute) - 30);
        }
    }
}
