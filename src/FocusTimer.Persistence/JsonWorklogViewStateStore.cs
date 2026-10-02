namespace FocusTimer.Persistence
{
    using System;
    using System.IO;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;

    /// <summary>
    /// Keeps the Worklog window's view preferences in a small JSON file next to the settings file. Reading a
    /// missing or damaged file gives an empty state, and a failed write is logged and ignored: these are
    /// conveniences, never data.
    /// </summary>
    public sealed class JsonWorklogViewStateStore : IWorklogViewStateStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly string _path;
        private readonly IAppLogger? _logger;
        private readonly SemaphoreSlim _gate = new(1, 1);

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonWorklogViewStateStore"/> class.
        /// </summary>
        /// <param name="path">The full path of the JSON file.</param>
        /// <param name="logger">An optional logger for diagnostics.</param>
        public JsonWorklogViewStateStore(string path, IAppLogger? logger = null)
        {
            this._path = path;
            this._logger = logger;
        }

        /// <inheritdoc/>
        public async Task<WorklogViewState> LoadAsync(CancellationToken cancellationToken = default)
        {
            await this._gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!File.Exists(this._path))
                {
                    return new WorklogViewState();
                }

                await using var stream = File.OpenRead(this._path);
                return await JsonSerializer.DeserializeAsync<WorklogViewState>(stream, JsonOptions, cancellationToken).ConfigureAwait(false)
                    ?? new WorklogViewState();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                this._logger?.LogWarning("The remembered Worklog view settings could not be read and are ignored: " + ex.Message);
                return new WorklogViewState();
            }
            finally
            {
                this._gate.Release();
            }
        }

        /// <inheritdoc/>
        public async Task SaveAsync(WorklogViewState state, CancellationToken cancellationToken = default)
        {
            await this._gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var directory = Path.GetDirectoryName(this._path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Write beside the file and swap, so a crash never leaves a half-written file behind.
                var temp = this._path + ".tmp";
                await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(state, JsonOptions), cancellationToken).ConfigureAwait(false);
                File.Move(temp, this._path, overwrite: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                this._logger?.LogWarning("The Worklog view settings could not be saved: " + ex.Message);
            }
            finally
            {
                this._gate.Release();
            }
        }
    }
}
