namespace FocusTimer.Core.Interfaces
{
    using System;

    /// <summary>
    /// Simple in-process event bus for decoupled publish/subscribe of application events.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// Publish an event to all subscribers.
        /// </summary>
        /// <typeparam name="T">Event type.</typeparam>
        /// <param name="message">Event message.</param>
        void Publish<T>(T message);

        /// <summary>
        /// Subscribe to events of type T.
        /// </summary>
        /// <typeparam name="T">Event type.</typeparam>
        /// <param name="handler">Handler to invoke when event is published.</param>
        /// <returns>An <see cref="IDisposable"/> subscription to cancel.</returns>
        IDisposable Subscribe<T>(Action<T> handler);
    }
}
