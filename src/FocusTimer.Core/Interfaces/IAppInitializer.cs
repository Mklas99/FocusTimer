namespace FocusTimer.Core.Interfaces
{
    using System.Threading.Tasks;

    /// <summary>
    /// Service for initializing the Avalonia application with injected dependencies.
    /// Decouples host startup from UI initialization logic.
    /// </summary>
    public interface IAppInitializer
    {
        /// <summary>
        /// Initialize the application with required services.
        /// Called by the host before Avalonia framework initialization completes.
        /// </summary>
        /// <param name="appController">The app controller instance.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="trayIconController">The tray icon controller instance.</param>
        /// <param name="serviceProvider">The service provider for accessing stored services on shutdown.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task InitializeAsync(
            object? appController,
            IAppLogger? logger,
            object? trayIconController,
            object? serviceProvider);
    }
}
