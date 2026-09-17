namespace FocusTimer.Core.Tests;

using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

public class WorklogEntryTests
{
    [Fact]
    public void Entry_GivenValidValues_HasDerivedDurationAndValueEquality()
    {
        var entry = CreateEntry();

        Assert.Equal(TimeSpan.FromMinutes(5), entry.Duration);
        Assert.Equal(entry, entry with { });
        Assert.Empty(WorklogEntryValidator.Validate(entry));
    }

    [Fact]
    public void Validator_GivenEmptyEntryId_ReturnsFailure()
    {
        var entry = CreateEntry() with { EntryId = "" };

        Assert.NotEmpty(WorklogEntryValidator.Validate(entry));
    }

    [Fact]
    public void Validator_GivenEmptySessionId_ReturnsFailure()
    {
        var entry = CreateEntry() with { SessionId = "" };

        Assert.NotEmpty(WorklogEntryValidator.Validate(entry));
    }

    [Fact]
    public void Validator_GivenNonPositiveInterval_ReturnsFailure()
    {
        var entry = CreateEntry() with { EndedAt = CreateEntry().StartedAt };

        Assert.NotEmpty(WorklogEntryValidator.Validate(entry));
    }

    [Fact]
    public void Validator_GivenZeroRevision_ReturnsFailure()
    {
        var entry = CreateEntry() with { Revision = 0 };

        Assert.NotEmpty(WorklogEntryValidator.Validate(entry));
    }

    [Fact]
    public void Validator_GivenCrossDayInterval_ReturnsFailure()
    {
        var entry = CreateEntry() with { EndedAt = CreateEntry().EndedAt.AddDays(1) };

        Assert.NotEmpty(WorklogEntryValidator.Validate(entry));
    }

    [Fact]
    public void ControlledValues_HaveStableStorageNames()
    {
        Assert.Equal("application-change", WorklogValueCodec.ToStoredValue(EndReason.ApplicationChange));
        Assert.Equal("windows", WorklogValueCodec.ToStoredValue(SourcePlatform.Windows));
    }

    [Fact]
    public void SourcePlatformProvider_GivenWindowsRuntime_ReturnsWindows()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Equal(SourcePlatform.Windows, new SourcePlatformProvider().GetCurrentPlatform());
        }
    }

    internal static TimeEntry CreateEntry() => new("entry", "session", new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 31, 9, 5, 0, TimeSpan.Zero), "Code", "FocusTimer", null, ProjectAssignmentSource.Unassigned, null, ActivityKind.Active, EndReason.ManualPause, CaptureSource.ActiveWindow, SourcePlatform.Windows, "device", 1, DateTimeOffset.UtcNow);
}
