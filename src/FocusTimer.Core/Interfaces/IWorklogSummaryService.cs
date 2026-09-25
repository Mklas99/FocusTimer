namespace FocusTimer.Core.Interfaces;

using FocusTimer.Core.Models;

/// <summary>Summarizes worklog entries into grouped totals.</summary>
public interface IWorklogSummaryService
{
    /// <summary>Summarizes the entries described by a request.</summary>
    /// <param name="request">What to summarize.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The summary; a failed read is reported in the outcome, never as an empty success.</returns>
    Task<WorklogSummary> SummarizeAsync(WorklogSummaryRequest request, CancellationToken cancellationToken = default);
}
