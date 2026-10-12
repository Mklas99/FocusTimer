namespace FocusTimer.App.Tests;

using FocusTimer.App.Services;

public class WidgetBackdropCapabilityTests
{
    [Fact]
    public void MissingExtensionAndAttachmentFailure_AreUnavailable()
    {
        var absent = new WidgetBackdropCapability(null);
        Assert.False(absent.IsAvailable);
        Assert.Null(absent.TryAttach(new IntPtr(1)));
        var failing = new WidgetBackdropCapability(typeof(FailingController));
        Assert.False(failing.IsAvailable);
        Assert.Null(failing.TryAttach(new IntPtr(1)));
        Assert.Null(failing.TryAttach(IntPtr.Zero));
        Assert.Null(new WidgetBackdropCapability(typeof(ThrowingController)).TryAttach(new IntPtr(1)));
    }

    [Fact]
    public void Completion_ReportsFailureAndIgnoresSupersededOrDisposedWork()
    {
        using var session = new WidgetBackdropCapability(typeof(FakeController)).TryAttach(new IntPtr(1));
        var results = new List<bool>();
        Assert.True(session!.TryUpdate(new IntPtr(1), 8, 1, true, results.Add));
        Action<bool> obsolete = FakeController.Last!.Completion!;
        Assert.True(session.TryUpdate(new IntPtr(1), 12, 0.5f, true, results.Add));
        obsolete(true);
        Assert.Empty(results);
        Action<bool> current = FakeController.Last.Completion!;
        current(false);
        current(true);
        Assert.Equal(new[] { false, true }, results);
        session.Dispose();
        current(false);
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void PublicCapability_RejectsObsoleteHandlesAndDisposedUpdates()
    {
        var capability = new WidgetBackdropCapability(typeof(FakeController));
        using var session = capability.TryAttach(new IntPtr(1));
        Assert.NotNull(session);
        Assert.True(session.TryUpdate(new IntPtr(1), 12, 0.5f, true));
        Assert.True(session.IsApplied);
        Assert.False(session.TryUpdate(new IntPtr(2), 8, 1, true));
        Assert.False(session.TryUpdate(IntPtr.Zero, 8, 1, true));
        Assert.False(session.TryUpdate(new IntPtr(1), -1, 1, true));
        session.Dispose();
        session.Dispose();
        Assert.False(session.TryUpdate(new IntPtr(1), 8, 1, true));
        Assert.False(session.IsApplied);
        Assert.Equal(1, FakeController.Last!.DisposeCount);
        Assert.Equal((12f, 0.5f, true), FakeController.Last.Update);
    }

    public sealed class FailingController
    {
        public static bool TryCreate(IntPtr handle, out FailingController? controller)
        {
            controller = null;
            return false;
        }
    }

    public sealed class ThrowingController
    {
        public static bool TryCreate(IntPtr handle, out ThrowingController? controller) => throw new InvalidOperationException("Attachment failed");
    }

    public sealed class FakeController : IDisposable
    {
        public static FakeController? Last { get; private set; }

        public int DisposeCount { get; private set; }

        public bool IsApplied => this.DisposeCount == 0;

        public Action<bool>? Completion { get; private set; }

        public bool TryUpdateWithCompletion(float radius, float opacity, bool enabled, Action<bool> completed)
        {
            this.Completion = completed;
            return this.TryUpdate(radius, opacity, enabled);
        }

        public (float, float, bool) Update { get; private set; }

        public static bool TryCreate(IntPtr handle, out FakeController? controller)
        {
            Last = controller = new FakeController();
            return true;
        }

        public bool TryUpdate(float radius, float opacity, bool enabled)
        {
            if (radius < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(radius));
            }

            this.Update = (radius, opacity, enabled);
            return true;
        }

        public void Dispose() => this.DisposeCount++;
    }
}
