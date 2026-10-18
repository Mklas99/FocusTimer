namespace FocusTimer.App.Tests;

using System;
using System.Collections.Generic;
using FocusTimer.App.Services;

public class WidgetBackdropCapabilityTests
{
    [Fact]
    public void DefaultConstructor_InitializesCorrectly()
    {
        var capability = new WidgetBackdropCapability();
        Assert.NotNull(capability);
        Assert.Null(capability.TryAttach(IntPtr.Zero));
    }

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
    public void IsAvailable_WhenControllerTypeMissingRequiredMethods_ReturnsFalse()
    {
        Assert.False(new WidgetBackdropCapability(typeof(object)).IsAvailable);
        Assert.False(new WidgetBackdropCapability(typeof(MissingTryCreateController)).IsAvailable);
    }

    [Fact]
    public void TryAttach_WhenCreateReturnsFalse_ReturnsNull()
    {
        var capability = new WidgetBackdropCapability(typeof(CreateReturnsFalseController));
        Assert.True(capability.IsAvailable);
        Assert.Null(capability.TryAttach(new IntPtr(1)));
    }

    [Fact]
    public void TryAttach_WhenCreateReturnsFalseWithDisposableController_DisposesControllerAndReturnsNull()
    {
        CreateFailsWithDisposableController.LastDisposed = false;
        var capability = new WidgetBackdropCapability(typeof(CreateFailsWithDisposableController));
        Assert.True(capability.IsAvailable);
        Assert.Null(capability.TryAttach(new IntPtr(1)));
        Assert.True(CreateFailsWithDisposableController.LastDisposed);
    }

    [Fact]
    public void TryAttach_WhenControllerNotDisposable_ReturnsNull()
    {
        var capability = new WidgetBackdropCapability(typeof(NonDisposableController));
        Assert.True(capability.IsAvailable);
        Assert.Null(capability.TryAttach(new IntPtr(1)));
    }

    [Fact]
    public void TryAttach_WhenIsAppliedMissing_DisposesControllerAndReturnsNull()
    {
        MissingIsAppliedController.LastDisposed = false;
        var capability = new WidgetBackdropCapability(typeof(MissingIsAppliedController));
        Assert.True(capability.IsAvailable);
        Assert.Null(capability.TryAttach(new IntPtr(1)));
        Assert.True(MissingIsAppliedController.LastDisposed);
    }

    [Fact]
    public void TryAttach_WhenMemberSignatureMismatch_CatchesExceptionDisposesAndReturnsNull()
    {
        WrongSignatureController.LastDisposed = false;
        var capability = new WidgetBackdropCapability(typeof(WrongSignatureController));
        Assert.True(capability.IsAvailable);
        Assert.Null(capability.TryAttach(new IntPtr(1)));
        Assert.True(WrongSignatureController.LastDisposed);
    }

    [Fact]
    public void TryAttach_WhenTryCreateThrows_ReturnsNull()
    {
        var capability = new WidgetBackdropCapability(typeof(ThrowingCreateController));
        Assert.True(capability.IsAvailable);
        Assert.Null(capability.TryAttach(new IntPtr(1)));
    }

    [Fact]
    public void Session_IsApplied_ReturnsFalseWhenControllerReportsNotApplied()
    {
        var capability = new WidgetBackdropCapability(typeof(FakeController));
        using var session = capability.TryAttach(new IntPtr(1));
        Assert.NotNull(session);
        FakeController.Last!.AppliedState = false;
        Assert.False(session.IsApplied);
        FakeController.Last.AppliedState = true;
        Assert.True(session.IsApplied);
    }

    [Fact]
    public void Session_TryUpdate_HandlesNullCompletionActionAndDisposedCallback()
    {
        var capability = new WidgetBackdropCapability(typeof(FakeController));
        using var session = capability.TryAttach(new IntPtr(1));
        Assert.NotNull(session);
        // Call TryUpdate without a completion action
        Assert.True(session.TryUpdate(new IntPtr(1), 10, 0.8f, true, null));
        Action<bool>? completion = FakeController.Last!.Completion;
        Assert.NotNull(completion);
        // Invoking completion when completed == null should not throw
        completion(true);

        // Test completion when session is already disposed
        var completedInvoked = false;
        using (var tempSession = capability.TryAttach(new IntPtr(2)))
        {
            Assert.True(tempSession!.TryUpdate(new IntPtr(2), 10, 0.8f, true, _ => completedInvoked = true));
            Action<bool> tempCompletion = FakeController.Last!.Completion!;
            tempSession.Dispose();
            tempCompletion(true);
            Assert.False(completedInvoked);
        }
    }

    [Fact]
    public void Session_TryUpdate_CatchesInvalidOperationException_ReturnsFalse()
    {
        var capability = new WidgetBackdropCapability(typeof(FakeController));
        using var session = capability.TryAttach(new IntPtr(1));
        Assert.NotNull(session);
        Assert.False(session.TryUpdate(new IntPtr(1), -2, 1, true));
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

    public sealed class MissingTryCreateController
    {
        public bool TryUpdateWithCompletion(float radius, float opacity, bool enabled, Action<bool> completed) => true;
    }

    public sealed class CreateReturnsFalseController : IDisposable
    {
        public static bool TryCreate(IntPtr handle, out CreateReturnsFalseController? controller)
        {
            controller = null;
            return false;
        }

        public bool TryUpdateWithCompletion(float radius, float opacity, bool enabled, Action<bool> completed) => true;
        public bool IsApplied => true;
        public void Dispose() { }
    }

    public sealed class CreateFailsWithDisposableController : IDisposable
    {
        public static bool LastDisposed { get; set; }

        public static bool TryCreate(IntPtr handle, out CreateFailsWithDisposableController? controller)
        {
            controller = new CreateFailsWithDisposableController();
            return false;
        }

        public bool TryUpdateWithCompletion(float radius, float opacity, bool enabled, Action<bool> completed) => true;
        public bool IsApplied => true;
        public void Dispose() => LastDisposed = true;
    }

    public sealed class NonDisposableController
    {
        public static bool TryCreate(IntPtr handle, out NonDisposableController? controller)
        {
            controller = new NonDisposableController();
            return true;
        }

        public bool TryUpdateWithCompletion(float radius, float opacity, bool enabled, Action<bool> completed) => true;
        public bool IsApplied => true;
    }

    public sealed class MissingIsAppliedController : IDisposable
    {
        public static bool LastDisposed { get; set; }

        public static bool TryCreate(IntPtr handle, out MissingIsAppliedController? controller)
        {
            controller = new MissingIsAppliedController();
            return true;
        }

        public bool TryUpdateWithCompletion(float radius, float opacity, bool enabled, Action<bool> completed) => true;
        public void Dispose() => LastDisposed = true;
    }

    public sealed class WrongSignatureController : IDisposable
    {
        public static bool LastDisposed { get; set; }

        public static bool TryCreate(IntPtr handle, out WrongSignatureController? controller)
        {
            controller = new WrongSignatureController();
            return true;
        }

        public string IsApplied => "not a bool";
        public bool TryUpdateWithCompletion(float radius, float opacity, bool enabled, Action<bool> completed) => true;
        public void Dispose() => LastDisposed = true;
    }

    public sealed class ThrowingCreateController : IDisposable
    {
        public static bool TryCreate(IntPtr handle, out ThrowingCreateController? controller)
            => throw new InvalidOperationException("Failed during creation");

        public bool TryUpdateWithCompletion(float radius, float opacity, bool enabled, Action<bool> completed) => true;
        public bool IsApplied => true;
        public void Dispose() { }
    }

    public sealed class FakeController : IDisposable
    {
        public static FakeController? Last { get; private set; }

        public int DisposeCount { get; private set; }

        public bool AppliedState { get; set; } = true;

        public bool IsApplied => this.AppliedState && this.DisposeCount == 0;

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
            if (radius == -1)
            {
                throw new ArgumentOutOfRangeException(nameof(radius));
            }

            if (radius == -2)
            {
                throw new InvalidOperationException("Update invalid operation");
            }

            this.Update = (radius, opacity, enabled);
            return true;
        }

        public void Dispose() => this.DisposeCount++;
    }
}
