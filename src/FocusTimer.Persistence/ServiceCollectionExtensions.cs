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

            services.AddSingleton<ISessionRepository>(sp =>
            {
                var settingsProvider = sp.GetRequiredService<ISettingsProvider>();
                var logger = sp.GetService<IAppLogger>();
                return new CsvSessionRepository(settingsProvider, logger);
            });

            return services;
        }
    }
}
