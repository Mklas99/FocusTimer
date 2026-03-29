namespace FocusTimer.Core.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using FocusTimer.Core.Interfaces;

    /// <summary>
    /// Thread-safe in-memory event bus.
    /// </summary>
    public class EventBus : IEventBus
    {
        private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

        /// <inheritdoc/>
        public void Publish<T>(T message)
        {
            if (this._handlers.TryGetValue(typeof(T), out var list))
            {
                // Make a copy to avoid modification during iteration
                var copy = list.ToArray();
                foreach (var d in copy)
                {
                    try
                    {
                        ((Action<T>)d)?.Invoke(message);
                    }
                    catch
                    {
                        // swallow to avoid breaking publisher
                    }
                }
            }
        }

        /// <inheritdoc/>
        public IDisposable Subscribe<T>(Action<T> handler)
        {
            var list = this._handlers.GetOrAdd(typeof(T), _ => new List<Delegate>());
            lock (list)
            {
                list.Add(handler);
            }

            return new Subscription<T>(this._handlers, handler);
        }

        private sealed class Subscription<T> : IDisposable
        {
            private readonly ConcurrentDictionary<Type, List<Delegate>> _dict;
            private readonly Action<T> _handler;

            public Subscription(ConcurrentDictionary<Type, List<Delegate>> dict, Action<T> handler)
            {
                this._dict = dict;
                this._handler = handler;
            }

            public void Dispose()
            {
                if (this._dict.TryGetValue(typeof(T), out var list))
                {
                    lock (list)
                    {
                        list.Remove(this._handler);
                    }
                }
            }
        }
    }
}
