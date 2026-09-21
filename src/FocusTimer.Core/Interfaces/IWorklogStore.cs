#pragma warning disable

namespace FocusTimer.Core.Interfaces;

using FocusTimer.Core.Models;
/// <summary>Storage-neutral contract for durable worklog entries.</summary>
public interface IWorklogStore
{
    Task<WorklogOutcome> AppendAsync(IReadOnlyCollection<TimeEntry> entries, CancellationToken cancellationToken = default);
    Task<WorklogReadResult> GetAsync(string entryId, CancellationToken cancellationToken = default);
    Task<WorklogReadResult> QueryAsync(WorklogQuery query, CancellationToken cancellationToken = default);
    Task<WorklogOutcome> PatchAsync(string entryId, int expectedRevision, WorklogPatch patch, CancellationToken cancellationToken = default);
    Task<WorklogOutcome> DeleteAsync(string entryId, int expectedRevision, CancellationToken cancellationToken = default);
}
