namespace FocusTimer.Persistence;

/// <summary>Performs the filesystem portion of an atomic daily-worklog replacement.</summary>
public interface IAtomicWorklogFileOperations
{
    /// <summary>Creates an exclusive temporary stream in the target's directory.</summary>
    /// <param name="targetPath">The finalized worklog path.</param>
    /// <param name="temporaryPath">The generated same-directory temporary path.</param>
    /// <returns>An exclusive stream for the temporary file.</returns>
    FileStream CreateTemporaryFile(string targetPath, out string temporaryPath);

    /// <summary>Activates a fully validated replacement without exposing a partial target.</summary>
    /// <param name="temporaryPath">The fully written and validated temporary file.</param>
    /// <param name="targetPath">The finalized worklog path to replace.</param>
    void ActivateReplacement(string temporaryPath, string targetPath);

    /// <summary>Removes a recognized temporary artifact when it is no longer required.</summary>
    /// <param name="temporaryPath">The recognized temporary artifact.</param>
    void DeleteTemporaryFile(string temporaryPath);

    /// <summary>Removes recognized stale artifacts for one target only.</summary>
    /// <param name="targetPath">The finalized worklog path whose artifacts may be removed.</param>
    void CleanupStaleTemporaryFiles(string targetPath);
}

/// <summary>Windows-safe same-directory implementation of atomic worklog replacement.</summary>
public sealed class AtomicWorklogFileOperations : IAtomicWorklogFileOperations
{
    private const string TemporarySuffix = ".worklog-rewrite-";
    private static readonly TimeSpan StaleTemporaryAge = TimeSpan.FromMinutes(5);

    /// <inheritdoc/>
    public FileStream CreateTemporaryFile(string targetPath, out string temporaryPath)
    {
        string directory = Path.GetDirectoryName(targetPath) ?? throw new ArgumentException("A target directory is required.", nameof(targetPath));
        Directory.CreateDirectory(directory);
        temporaryPath = Path.Combine(directory, Path.GetFileName(targetPath) + TemporarySuffix + Guid.NewGuid().ToString("N") + ".tmp");
        return new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough);
    }

    /// <inheritdoc/>
    public void ActivateReplacement(string temporaryPath, string targetPath)
    {
        if (File.Exists(targetPath))
        {
            File.Replace(temporaryPath, targetPath, null, ignoreMetadataErrors: true);
            return;
        }

        File.Move(temporaryPath, targetPath);
    }

    /// <inheritdoc/>
    public void DeleteTemporaryFile(string temporaryPath)
    {
        if (File.Exists(temporaryPath))
        {
            File.Delete(temporaryPath);
        }
    }

    /// <inheritdoc/>
    public void CleanupStaleTemporaryFiles(string targetPath)
    {
        string? directory = Path.GetDirectoryName(targetPath);
        if (directory is null || !Directory.Exists(directory))
        {
            return;
        }

        string pattern = Path.GetFileName(targetPath) + TemporarySuffix + "*.tmp";
        foreach (string path in Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly))
        {
            if (File.GetLastWriteTimeUtc(path) <= DateTime.UtcNow - StaleTemporaryAge)
            {
                this.DeleteTemporaryFile(path);
            }
        }
    }
}
