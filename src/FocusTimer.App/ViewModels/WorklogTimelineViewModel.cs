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
    using ReactiveUI;

    /// <summary>
    /// Shows the day that the Entries tab loaded as blocks along a time axis. It reads the same loaded list as the
    /// table, so the two can never disagree.
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

        private readonly WorklogEntriesViewModel _entries;
        private readonly IWorklogViewStateStore? _viewState;
        private readonly TimeSpan _saveDelay;
        private CancellationTokenSource? _saveCts;
        private bool _remembering = true;
        private IReadOnlyList<TimelineBlockViewModel> _blocks = [];
        private double _scrollTargetMinute;
        private double _hourHeight = DefaultHourHeight;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorklogTimelineViewModel"/> class.
        /// </summary>
        /// <param name="entries">The Entries tab whose loaded day is drawn.</param>
        /// <param name="viewState">Remembers the zoom between runs; without it the zoom starts at 100% every time.</param>
        /// <param name="saveDelay">How long the zoom must stay unchanged before it is saved; null means 400 ms.</param>
        public WorklogTimelineViewModel(
            WorklogEntriesViewModel entries,
            IWorklogViewStateStore? viewState = null,
            TimeSpan? saveDelay = null)
        {
            this._entries = entries;
            this._viewState = viewState;
            this._saveDelay = saveDelay ?? TimeSpan.FromMilliseconds(400);
            this._remembering = viewState is null;
            this._entries.PropertyChanged += this.OnEntriesChanged;
            this.ZoomInCommand = ReactiveCommand.Create(this.ZoomIn);
            this.ZoomOutCommand = ReactiveCommand.Create(this.ZoomOut);
            this.ResetZoomCommand = ReactiveCommand.Create(this.ResetZoom);
            this.Rebuild();
        }

        /// <summary>Gets the task of the most recent zoom save; used to await it.</summary>
        public Task PendingSave { get; private set; } = Task.CompletedTask;

        /// <summary>Gets the command that makes an hour taller.</summary>
        public ICommand ZoomInCommand { get; }

        /// <summary>Gets the command that makes an hour shorter.</summary>
        public ICommand ZoomOutCommand { get; }

        /// <summary>Gets the command that returns to the default scale.</summary>
        public ICommand ResetZoomCommand { get; }

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
                this.Blocks = Layout(this._entries.AllRows, this._hourHeight);
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
            double hourHeight = DefaultHourHeight)
        {
            var minimumDisplayMinutes = MinimumBlockHeight / hourHeight * 60;
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

            return placed.Select(p => new TimelineBlockViewModel(p.Row, p.Lane, groupLaneCounts[p.Group])).ToList();
        }

        /// <summary>
        /// Applies the zoom remembered from the last run. Changes the user makes afterwards are saved; the applied
        /// value itself is not written back.
        /// </summary>
        /// <returns>A task that completes when the remembered zoom has been applied.</returns>
        public async Task LoadViewStateAsync()
        {
            if (this._viewState is null)
            {
                return;
            }

            try
            {
                var state = await this._viewState.LoadAsync().ConfigureAwait(true);
                this._remembering = false;
                if (state.TimelineHourHeight is { } remembered && double.IsFinite(remembered))
                {
                    this._remembering = true;
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
                await this._viewState!.SaveAsync(new WorklogViewState(this._hourHeight), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // A newer zoom replaced this one; it will save instead.
            }
            catch (Exception)
            {
                // Remembering the zoom is a convenience; the store logs its own failure and the zoom still works.
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
            this.Blocks = Layout(this._entries.AllRows, this._hourHeight);
            this.ScrollTargetMinute = this._blocks.Count == 0
                ? 8 * 60
                : Math.Max(0, this._blocks.Min(b => b.StartMinute) - 30);
        }
    }
}
