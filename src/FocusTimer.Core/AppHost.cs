namespace FocusTimer.Core
{
    using System;

    /// <summary>
    /// Global host service accessor. The executable host should set <see cref="Services"/>
    /// during startup so libraries and UI can access the application's <see cref="IServiceProvider"/>.
    /// </summary>
    public static class AppHost
    {
        /// <summary>
        /// Gets or sets the application's root <see cref="IServiceProvider"/>.
        /// The host must assign this during startup.
        /// </summary>
        public static IServiceProvider Services { get; set; } = null!;
    }
}
