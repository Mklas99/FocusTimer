namespace FocusTimer.Core.Interfaces
{
    using System;

    /// <summary>
    /// Defines the contract for application logging.
    /// </summary>
    public interface IAppLogger
    {
        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void LogDebug(string message);

        /// <summary>
        /// Logs an information message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void LogInformation(string message);

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void LogWarning(string message);

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">The exception associated with the error, if any.</param>
        void LogError(string message, Exception? ex = null);

        /// <summary>
        /// Logs a critical message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">The exception associated with the critical error, if any.</param>
        void LogCritical(string message, Exception? ex = null);
    }
}
