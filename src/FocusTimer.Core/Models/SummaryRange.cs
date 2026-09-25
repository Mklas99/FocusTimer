namespace FocusTimer.Core.Models;

/// <summary>A half-open time range: start inclusive, end exclusive.</summary>
/// <param name="StartInclusive">The first instant that belongs to the range.</param>
/// <param name="EndExclusive">The first instant after the range.</param>
public sealed record SummaryRange(DateTimeOffset StartInclusive, DateTimeOffset EndExclusive)
{
    /// <summary>Gets a value indicating whether the range ends after it starts.</summary>
    public bool IsValid => this.EndExclusive > this.StartInclusive;
}
