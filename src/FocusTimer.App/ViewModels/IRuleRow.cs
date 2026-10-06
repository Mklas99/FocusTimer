namespace FocusTimer.App.ViewModels
{
    /// <summary>A row of an editable rule list that reports its own validation message.</summary>
    public interface IRuleRow
    {
        /// <summary>Gets validation feedback, empty when the row is valid.</summary>
        string Error { get; }

        /// <summary>Gets a value indicating whether every field is blank (the row is dropped when settings are committed).</summary>
        bool IsBlank { get; }

        /// <summary>Gets the notice shown for a blank row, or an empty string.</summary>
        string Warning { get; }

        /// <summary>Gets a value indicating whether <see cref="Warning"/> is shown.</summary>
        bool HasWarning { get; }

        /// <summary>Gets a value indicating whether <see cref="Error"/> is shown.</summary>
        bool HasError { get; }
    }
}
