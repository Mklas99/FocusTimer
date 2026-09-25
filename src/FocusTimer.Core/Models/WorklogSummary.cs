namespace FocusTimer.Core.Models;

/// <summary>The result of a summary request.</summary>
/// <param name="Request">The request that produced this result.</param>
/// <param name="Outcome">Success, or the reason the worklog could not be summarized.</param>
/// <param name="Rows">The groups, longest duration first. Empty when the outcome is not a success.</param>
/// <param name="Total">The sum of all row durations.</param>
/// <param name="Warnings">Diagnostics for data that was skipped but did not fail the read.</param>
public sealed record WorklogSummary(
    WorklogSummaryRequest Request,
    WorklogOutcome Outcome,
    IReadOnlyList<SummaryRow> Rows,
    TimeSpan Total,
    IReadOnlyList<WorklogWarning> Warnings)
{
    /// <summary>Gets a value indicating whether the worklog was read and summarized.</summary>
    public bool IsSuccess => this.Outcome.IsSuccess;
}
