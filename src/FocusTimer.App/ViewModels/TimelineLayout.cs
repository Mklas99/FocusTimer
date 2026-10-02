namespace FocusTimer.App.ViewModels
{
    using System.Collections.Generic;

    /// <summary>The placed blocks of a timeline and, when it is grouped, its columns.</summary>
    /// <param name="Blocks">Every entry as a block, in start order.</param>
    /// <param name="Groups">The columns; empty when the timeline is not grouped.</param>
    public sealed record TimelineLayout(
        IReadOnlyList<TimelineBlockViewModel> Blocks,
        IReadOnlyList<TimelineGroupViewModel> Groups);
}
