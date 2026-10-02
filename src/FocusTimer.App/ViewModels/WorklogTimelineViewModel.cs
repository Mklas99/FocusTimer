namespace FocusTimer.App.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;
    using FocusTimer.Core.Services;
    using ReactiveUI;

    /// <summary>
    /// Shows the day that the Entries tab loaded as blocks along a time axis, either all together or in one column per
    /// application, project, or window. It reads the same loaded list as the table, so the two can never disagree.
    /// </summary>
    public class WorklogTimelineViewModel : ReactiveObject
    {
        /// <summary>The height of one hour at the default zoom.</summary>
        public const double DefaultHourHeight = 60;

        /// <summary>The smallest hour height the user can zoom out to.</summary>
        public const double MinHourHeight = 30;

        /// <summary>The largest hour height the user can zoom in to.</summary>
        public const double MaxHourHeight = 480;

        /// <summary>The factor one zoom step multiplies or divides the hour height by.</summary>
        public const double ZoomStep = 1.25;

        /// <summary>The height of the smallest block, so very short entries stay visible.</summary>
        public const double MinimumBlockHeight = 20;

        /// <summary>The id of the choice that draws every entry in one column.</summary>
        public const string NoGroupingId = "";

        private readonly WorklogEntriesViewModel _entries;
        private readonly IWorklogViewStateStore? _viewState;
        private readonly TimeSpan _saveDelay;
        private readonly WorklogGroupingRegistry? _groupings;
        private readonly IProjectResolver _projectResolver;
        private CancellationTokenSource? _saveCts;
        private bool _remembering;
        private IReadOnlyList<TimelineBlockViewModel> _blocks = [];
        private IReadOnlyList<TimelineGroupViewModel> _groups = [];
        private GroupingOption _selectedGrouping;
        private double _scrollTargetMinute;
        private double _hourHeight = DefaultHourHeight;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorklogTimelineViewModel"/> class.
        /// </summary>
        /// <param name="entries">The Entries tab whose loaded day is drawn.</param>
        /// <param name="viewState">Remembers the zoom and grouping between runs; without it they start fresh every time.</param>
        /// <param name="saveDelay">How long a change must stay unchanged before it is saved; null means 400 ms.</param>
        /// <param name="groupings">The groupings the user can choose from; without them the timeline cannot be grouped.</param>
        /// <param name="projectResolver">Decides an entry's project for the project grouping; the stored tag by default.</param>
        public WorklogTimelineViewModel(
            WorklogEntriesViewModel entries,
            IWorklogViewStateStore? viewState = null,
            TimeSpan? saveDelay = null,
            WorklogGroupingRegistry? groupings = null,
            IProjectResolver? projectResolver = null)
        {
            this._entries = entries;
            this._entries.PropertyChanged += this.OnEntriesChanged;
            this._viewState = viewState;
            this._saveDelay = saveDelay ?? TimeSpan.FromMilliseconds(400);
            this._groupings = groupings;
            this._projectResolver = projectResolver ?? new StoredProjectResolver();
            this._remembering = viewState is null;
            this.GroupingOptions =
            [
                new GroupingOption(NoGroupingId, "All entries together"),
                .. (groupings?.All ?? []).Select(g => new GroupingOption(g.Id, g.DisplayName)),
            ];
            this._selectedGrouping = this.GroupingOptions[0];
            this.ZoomInCommand = ReactiveCommand.Create(this.ZoomIn);
            this.ZoomOutCommand = ReactiveCommand.Create(this.ZoomOut);
            this.ResetZoomCommand = ReactiveCommand.Create(this.ResetZoom);
            this.Rebuild();
        }

        /// <summary>Gets the task of the most recent save of the zoom or grouping; used to await it.</summary>
        public Task PendingSave { get; private set; } = Task.CompletedTask;

        /// <summary>Gets the command that makes an hour taller.</summary>
        public ICommand ZoomInCommand { get; }

        /// <summary>Gets the command that makes an hour shorter.</summary>
        public ICommand ZoomOutCommand { get; }

        /// <summary>Gets the command that returns to the default scale.</summary>
        public ICommand ResetZoomCommand { get; }

        /// <summary>Gets the choices for how the timeline is split: all together, or one column per group.</summary>
        public IReadOnlyList<GroupingOption> GroupingOptions { get; }

        /// <summary>
        /// Gets or sets how the timeline is split. "All entries together" draws one column; the others draw one column
        /// per application, project, or window, using the same groupings as the Summary.
        /// </summary>
        public GroupingOption SelectedGrouping
        {
            get => this._selectedGrouping;
            set
            {
                if (value is null || Equals(this._selectedGrouping, value) || !this.GroupingOptions.Contains(value))
                {
                    return;
                }

                this.RaiseAndSetIfChanged(ref this._selectedGrouping, value);
                this.RaisePropertyChanged(nameof(this.IsGrouped));
                this.Rebuild();
                this.QueueSave();
            }
        }

        /// <summary>Gets a value indicating whether the timeline is split into group columns.</summary>
        public bool IsGrouped => this._selectedGrouping.Id != NoGroupingId;

        /// <summary>Gets the group columns, left to right; empty when the timeline is not grouped.</summary>
        public IReadOnlyList<TimelineGroupViewModel> Groups
        {
            get => this._groups;
            private set
            {
                this.RaiseAndSetIfChanged(ref this._groups, value);
                this.RaisePropertyChanged(nameof(this.ColumnCount));
                this.RaisePropertyChanged(nameof(this.HasGroups));
            }
        }

        /// <summary>Gets a value indicating whether group columns are shown.</summary>
        public bool HasGroups => this._groups.Count > 0;

        /// <summary>Gets the number of columns the axis is split into; one when the timeline is not grouped.</summary>
        public int ColumnCount => Math.Max(1, this._groups.Count);

        /// <summary>
        /// Gets or sets the height of one hour on the axis. The value is kept between <see cref="MinHourHeight"/> and
        /// <see cref="MaxHourHeight"/>; a taller hour leaves more room for each entry's text.
        /// </summary>
        public double HourHeight
        {
            get => this._hourHeight;
            set
            {
                var clamped = Math.Clamp(value, MinHourHeight, MaxHourHeight);
                if (Math.Abs(clamped - this._hourHeight) < 0.001)
                {
                    return;
                }

                this.RaiseAndSetIfChanged(ref this._hourHeight, clamped);
                this.QueueSave();
                this.RaisePropertyChanged(nameof(this.AxisHeight));
                this.RaisePropertyChanged(nameof(this.ZoomText));
                this.RaisePropertyChanged(nameof(this.CanZoomIn));
                this.RaisePropertyChanged(nameof(this.CanZoomOut));
                this.Rebuild();
            }
        }

        /// <summary>Gets the total height of the 24 hour axis.</summary>
        public double AxisHeight => 24 * this._hourHeight;

        /// <summary>Gets the zoom as a percentage of the default, such as "100%".</summary>
        public string ZoomText => $"{Math.Round(this._hourHeight / DefaultHourHeight * 100):0}%";

        /// <summary>Gets a value indicating whether the hour can be made taller.</summary>
        public bool CanZoomIn => this._hourHeight < MaxHourHeight - 0.001;

        /// <summary>Gets a value indicating whether the hour can be made shorter.</summary>
        public bool CanZoomOut => this._hourHeight > MinHourHeight + 0.001;

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
        /// Works out the scroll offset that keeps the moment under a point in place when the scale changes, so
        /// zooming with the mouse wheel feels anchored to the pointer.
        /// </summary>
        /// <param name="offset">The current vertical scroll offset.</param>
        /// <param name="pointerY">The pointer's distance from the top of the visible area.</param>
        /// <param name="oldHourHeight">The hour height before the change.</param>
        /// <param name="newHourHeight">The hour height after the change.</param>
        /// <returns>The new vertical offset, never negative.</returns>
        public static double AnchoredOffset(double offset, double pointerY, double oldHourHeight, double newHourHeight)
        {
            var minuteUnderPointer = (offset + pointerY) / oldHourHeight * 60;
            return Math.Max(0, (minuteUnderPointer / 60 * newHourHeight) - pointerY);
        }

        /// <summary>
        /// Places entries into lanes: entries that overlap in time (at their displayed length) are drawn side by side.
        /// </summary>
        /// <param name="rows">The rows to place.</param>
        /// <param name="hourHeight">The hour height; it decides how many minutes the smallest block covers.</param>
        /// <returns>The blocks in start order.</returns>
        public static IReadOnlyList<TimelineBlockViewModel> Layout(
            IReadOnlyList<WorklogEntryRowViewModel> rows,
            double hourHeight = DefaultHourHeight) =>
            LayoutGroups(rows, hourHeight, null).Blocks;

        /// <summary>
        /// Places entries into one column per group (or one column when no grouping is given). Inside a column,
        /// entries that overlap in time are drawn side by side. Columns are ordered by total time, largest first, with the
        /// bucket for entries without a value last.
        /// </summary>
        /// <param name="rows">The rows to place.</param>
        /// <param name="hourHeight">The hour height; it decides how many minutes the smallest block covers.</param>
        /// <param name="groupOf">Decides each row's group; null draws everything in one column without group headers.</param>
        /// <returns>The blocks in start order and the group columns.</returns>
        public static TimelineLayout LayoutGroups(
            IReadOnlyList<WorklogEntryRowViewModel> rows,
            double hourHeight,
            Func<WorklogEntryRowViewModel, GroupKey>? groupOf)
        {
            var minimumDisplayMinutes = MinimumBlockHeight / hourHeight * 60;
            if (groupOf is null)
            {
                var single = PlaceLanes(rows, minimumDisplayMinutes);
                return new TimelineLayout(single.Select(p => new TimelineBlockViewModel(p.Row, p.Lane, p.LaneCount)).ToList(), []);
            }

            var buckets = rows
                .GroupBy(r => groupOf(r).Key)
                .Select(g =>
                {
                    var key = groupOf(g.First());
                    var total = TimeSpan.FromTicks(g.Sum(r => r.Entry.Duration.Ticks));
                    return (Key: key, Rows: g.ToList(), Total: total);
                })
                .OrderBy(b => b.Key.IsUnassigned)
                .ThenByDescending(b => b.Total)
                .ThenBy(b => b.Key.Label, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var groups = new List<TimelineGroupViewModel>();
            var blocks = new List<TimelineBlockViewModel>();
            for (var column = 0; column < buckets.Count; column++)
            {
                var bucket = buckets[column];
                groups.Add(new TimelineGroupViewModel(column, bucket.Key.Label, bucket.Key.IsUnassigned, bucket.Total, bucket.Rows.Count));
                blocks.AddRange(PlaceLanes(bucket.Rows, minimumDisplayMinutes)
                    .Select(p => new TimelineBlockViewModel(p.Row, p.Lane, p.LaneCount, column, buckets.Count)));
            }

            return new TimelineLayout(blocks.OrderBy(b => b.Row.Entry.StartedAt).ThenBy(b => b.Column).ToList(), groups);
        }

        /// <summary>
        /// Applies the zoom and grouping remembered from the last run. Changes the user makes afterwards are saved; the
        /// applied values themselves are not written back.
        /// </summary>
        /// <returns>A task that completes when the remembered values have been applied.</returns>
        public async Task LoadViewStateAsync()
        {
            if (this._viewState is null)
            {
                return;
            }

            try
            {
                var state = await this._viewState.LoadAsync().ConfigureAwait(true);
                this._remembering = true;
                if (state.TimelineGroupingId is { } groupingId
                    && this.GroupingOptions.FirstOrDefault(o => string.Equals(o.Id, groupingId, StringComparison.OrdinalIgnoreCase)) is { } option)
                {
                    this.SelectedGrouping = option;
                }

                if (state.TimelineHourHeight is { } remembered && double.IsFinite(remembered))
                {
                    this.HourHeight = remembered;
                }
            }
            finally
            {
                this._remembering = false;
            }
        }

        /// <summary>Makes an hour one step taller.</summary>
        public void ZoomIn() => this.HourHeight *= ZoomStep;

        /// <summary>Makes an hour one step shorter.</summary>
        public void ZoomOut() => this.HourHeight /= ZoomStep;

        /// <summary>Returns to the default scale.</summary>
        public void ResetZoom() => this.HourHeight = DefaultHourHeight;

        private static List<(WorklogEntryRowViewModel Row, int Lane, int LaneCount)> PlaceLanes(
            IReadOnlyList<WorklogEntryRowViewModel> rows,
            double minimumDisplayMinutes)
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
                var end = start + Math.Max(row.Entry.Duration.TotalMinutes, minimumDisplayMinutes);
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

            return placed.Select(p => (p.Row, p.Lane, groupLaneCounts[p.Group])).ToList();
        }

        private void QueueSave()
        {
            if (this._viewState is null || this._remembering)
            {
                return;
            }

            this._saveCts?.Cancel();
            this._saveCts?.Dispose();
            var cts = new CancellationTokenSource();
            this._saveCts = cts;
            this.PendingSave = this.SaveAfterDelayAsync(cts.Token);
        }

        private async Task SaveAfterDelayAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(this._saveDelay, cancellationToken).ConfigureAwait(false);
                var grouping = this._selectedGrouping.Id == NoGroupingId ? null : this._selectedGrouping.Id;
                await this._viewState!.SaveAsync(new WorklogViewState(this._hourHeight, grouping), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // A newer change replaced this one; it will save instead.
            }
            catch (Exception)
            {
                // Remembering the view is a convenience; the store logs its own failure and the timeline still works.
            }
        }

        private void OnEntriesChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WorklogEntriesViewModel.AllRows))
            {
                this.Rebuild();
            }
        }

        private void Rebuild()
        {
            Func<WorklogEntryRowViewModel, GroupKey>? groupOf = null;
            if (this.IsGrouped && this._groupings is not null && this._groupings.TryGet(this._selectedGrouping.Id, out var grouping))
            {
                groupOf = row => grouping.Select(row.Entry, new GroupingContext(this._projectResolver.Resolve(row.Entry)));
            }

            var layout = LayoutGroups(this._entries.AllRows, this._hourHeight, groupOf);
            this.Blocks = layout.Blocks;
            this.Groups = layout.Groups;
            this.ScrollTargetMinute = this._blocks.Count == 0
                ? 8 * 60
                : Math.Max(0, this._blocks.Min(b => b.StartMinute) - 30);
        }
    }
}
