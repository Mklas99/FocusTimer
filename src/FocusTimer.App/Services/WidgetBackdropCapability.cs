namespace FocusTimer.App.Services
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Optional binding to the fork's versioned public API. No private framework visuals are accessed.
    /// Avalonia retains native ownership and queues updates through its composition commit loop.
    /// </summary>
    internal sealed class WidgetBackdropCapability
    {
        private readonly Type? _controllerType;

        /// <summary>Initializes a new instance of the <see cref="WidgetBackdropCapability"/> class using the optional backend.</summary>
        internal WidgetBackdropCapability()
            : this(OperatingSystem.IsWindows()
                ? Type.GetType("Avalonia.Win32.Win32BackdropControllerV1, Avalonia.Win32", throwOnError: false)
                : null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="WidgetBackdropCapability"/> class.</summary>
        /// <param name="controllerType">The public controller type.</param>
        internal WidgetBackdropCapability(Type? controllerType)
        {
            this._controllerType = controllerType;
        }

        /// <summary>Gets a value indicating whether the backend exposes the versioned public entry point.</summary>
        internal bool IsAvailable => this._controllerType?.GetMethod(
            "TryCreate",
            BindingFlags.Public | BindingFlags.Static,
            [typeof(IntPtr), this._controllerType.MakeByRefType()]) != null &&
            this._controllerType.GetMethod("TryUpdateWithCompletion", [typeof(float), typeof(float), typeof(bool), typeof(Action<bool>)]) != null;

        /// <summary>Attaches to a live window on Avalonia's UI thread.</summary>
        /// <param name="handle">Current native window handle.</param>
        /// <returns>A controller binding, or null when unavailable.</returns>
        internal Session? TryAttach(IntPtr handle)
        {
            if (handle == IntPtr.Zero || !this.IsAvailable)
            {
                return null;
            }

            object? controller = null;
            try
            {
                object?[] arguments = [handle, null];
                bool created = this._controllerType!.GetMethod("TryCreate", BindingFlags.Public | BindingFlags.Static)!
                    .Invoke(null, arguments) is true;
                controller = arguments[1];
                if (!created || controller is not IDisposable disposable)
                {
                    (controller as IDisposable)?.Dispose();
                    return null;
                }

                MethodInfo? update = this._controllerType.GetMethod(
                    "TryUpdateWithCompletion", [typeof(float), typeof(float), typeof(bool), typeof(Action<bool>)]);
                MethodInfo? applied = this._controllerType.GetProperty("IsApplied")?.GetMethod;
                if (update == null || applied == null)
                {
                    disposable.Dispose();
                    return null;
                }

                var callback = update.CreateDelegate<Func<float, float, bool, Action<bool>, bool>>(controller);
                var isApplied = applied.CreateDelegate<Func<bool>>(controller);
                return new Session(handle, disposable, callback, isApplied);
            }
            catch (Exception ex) when (ex is TargetInvocationException or ArgumentException or MemberAccessException)
            {
                (controller as IDisposable)?.Dispose();
                return null;
            }
        }

        /// <summary>A binding whose updates are valid only for its original handle.</summary>
        internal sealed class Session : IDisposable
        {
            private readonly IDisposable _controller;
            private readonly Func<float, float, bool, Action<bool>, bool> _update;
            private readonly Func<bool> _isApplied;
            private bool _disposed;
            private long _generation;

            /// <summary>Initializes a new instance of the <see cref="Session"/> class.</summary>
            /// <param name="handle">Original window handle.</param>
            /// <param name="controller">Avalonia's controller.</param>
            /// <param name="update">Public update method.</param>
            /// <param name="isApplied">Public queued-update completion query.</param>
            internal Session(
                IntPtr handle,
                IDisposable controller,
                Func<float, float, bool, Action<bool>, bool> update,
                Func<bool> isApplied)
            {
                this.Handle = handle;
                this._controller = controller;
                this._update = update;
                this._isApplied = isApplied;
            }

            /// <summary>Gets the handle this binding controls.</summary>
            internal IntPtr Handle { get; }

            /// <summary>Gets a value indicating whether the latest queued update completed, not proof of visible blur.</summary>
            internal bool IsApplied => !this._disposed && this._isApplied();

            /// <inheritdoc/>
            public void Dispose()
            {
                if (this._disposed)
                {
                    return;
                }

                this._disposed = true;
                this._controller.Dispose();
            }

            /// <summary>Queues backdrop state, rejecting disposed or obsolete window bindings.</summary>
            /// <param name="currentHandle">The window's current handle.</param>
            /// <param name="physicalRadius">Corner radius in physical pixels.</param>
            /// <param name="opacity">Overall widget opacity.</param>
            /// <param name="enabled">Whether the backdrop should render.</param>
            /// <param name="completed">Completion or failure notification for the latest update only.</param>
            /// <returns>Whether Avalonia accepted the update.</returns>
            internal bool TryUpdate(IntPtr currentHandle, float physicalRadius, float opacity, bool enabled, Action<bool>? completed = null)
            {
                if (this._disposed || currentHandle == IntPtr.Zero || currentHandle != this.Handle)
                {
                    return false;
                }

                try
                {
                    long generation = ++this._generation;
                    return this._update(physicalRadius, opacity, enabled, applied =>
                    {
                        if (!this._disposed && generation == this._generation)
                        {
                            completed?.Invoke(applied);
                        }
                    });
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    return false;
                }
            }
        }
    }
}
