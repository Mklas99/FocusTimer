namespace FocusTimer.Core.Tests;

using FocusTimer.Core.Models;

public class WorklogContractsTests
{
    public static TheoryData<Enum, string> StoredValues => new()
    {
        { ActivityKind.Active, "active" },
        { EndReason.ApplicationChange, "application-change" },
        { EndReason.ManualPause, "manual-pause" },
        { EndReason.IdlePause, "idle-pause" },
        { EndReason.DayBoundary, "day-boundary" },
        { EndReason.ApplicationExit, "application-exit" },
        { EndReason.Unknown, "unknown" },
        { CaptureSource.ActiveWindow, "active-window" },
        { SourcePlatform.Windows, "windows" },
        { SourcePlatform.Linux, "linux" },
        { SourcePlatform.MacOS, "macos" },
        { SourcePlatform.Unknown, "unknown" },
        { ProjectAssignmentSource.Unassigned, "unassigned" },
        { ProjectAssignmentSource.Session, "session" },
        { ProjectAssignmentSource.Rule, "rule" },
        { ProjectAssignmentSource.Editor, "editor" },
        { ProjectAssignmentSource.Imported, "imported" },
    };

    [Theory]
    [MemberData(nameof(StoredValues))]
    public void ToStoredValue_GivenControlledValue_ReturnsStableSpelling(Enum value, string expected)
    {
        var actual = value switch
        {
            ActivityKind v => WorklogValueCodec.ToStoredValue(v),
            EndReason v => WorklogValueCodec.ToStoredValue(v),
            CaptureSource v => WorklogValueCodec.ToStoredValue(v),
            SourcePlatform v => WorklogValueCodec.ToStoredValue(v),
            ProjectAssignmentSource v => WorklogValueCodec.ToStoredValue(v),
            _ => throw new InvalidOperationException("Unexpected enum type."),
        };

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToStoredValue_GivenUndefinedEnumValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WorklogValueCodec.ToStoredValue((EndReason)999));
    }

    [Fact]
    public void WorklogQuery_IsValid_RequiresEndAfterStart()
    {
        var start = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

        Assert.True(new WorklogQuery(start, start.AddHours(1)).IsValid);
        Assert.False(new WorklogQuery(start, start).IsValid);
        Assert.False(new WorklogQuery(start, start.AddHours(-1)).IsValid);
    }

    [Fact]
    public void WorklogOutcome_Success_IsSuccessAndCarriesWarnings()
    {
        var warnings = new[] { new WorklogWarning("a.jsonl", 3, "skipped") };

        var outcome = WorklogOutcome.Success(warnings);

        Assert.True(outcome.IsSuccess);
        Assert.Equal(warnings, outcome.Warnings);
    }

    [Theory]
    [InlineData(WorklogOutcomeKind.ValidationFailure)]
    [InlineData(WorklogOutcomeKind.NotFound)]
    [InlineData(WorklogOutcomeKind.Conflict)]
    [InlineData(WorklogOutcomeKind.UnsupportedSchema)]
    [InlineData(WorklogOutcomeKind.MalformedData)]
    [InlineData(WorklogOutcomeKind.IoFailure)]
    [InlineData(WorklogOutcomeKind.FileInUse)]
    public void WorklogOutcome_NonSuccessKinds_AreNotSuccess(WorklogOutcomeKind kind)
    {
        Assert.False(new WorklogOutcome(kind, "failed").IsSuccess);
    }

    [Fact]
    public void WorklogPatch_HasValueEquality()
    {
        var start = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

        WorklogPatch Make() => new(
            start, start.AddMinutes(5), "app", "title", null,
            ProjectAssignmentSource.Unassigned, null, ActivityKind.Active, EndReason.Unknown, CaptureSource.ActiveWindow);

        Assert.Equal(Make(), Make());
        Assert.NotEqual(Make(), Make() with { AppName = "other" });
    }
}
