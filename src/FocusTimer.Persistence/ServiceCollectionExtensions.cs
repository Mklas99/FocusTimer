namespace FocusTimer.Persistence
{
    using FocusTimer.Core.Interfaces;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Registers persistence-related services into an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds persistence implementations (settings provider and session repository).
        /// </summary>
        /// <param name="services">The service collection to register services into.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddPersistenceServices(this IServiceCollection services)
        {
            services.AddSingleton<ISettingsProvider, JsonSettingsProvider>();

            services.AddSingleton<IWorklogStore>(sp =>
            {
                ISettingsProvider settingsProvider = sp.GetRequiredService<ISettingsProvider>();
                IAppLogger? logger = sp.GetService<IAppLogger>();
                return new CsvSessionRepository(settingsProvider, logger);
            });

            services.AddSingleton<IWorklogViewStateStore>(sp =>
            {
                // Beside the settings file, so the remembered view follows the same profile.
                var settingsDirectory = sp.GetRequiredService<ISettingsProvider>() is JsonSettingsProvider json
                    ? System.IO.Path.GetDirectoryName(json.SettingsFilePath)
                    : null;
                settingsDirectory ??= System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FocusTimer");
                return new JsonWorklogViewStateStore(
                    System.IO.Path.Combine(settingsDirectory, "worklog-view.json"), sp.GetService<IAppLogger>());
            });

            return services;
        }
    }
}
