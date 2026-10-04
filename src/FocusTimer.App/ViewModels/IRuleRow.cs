namespace FocusTimer.App.ViewModels
{
    /// <summary>A row of an editable rule list that reports its own validation message.</summary>
    public interface IRuleRow
    {
        /// <summary>Gets validation feedback, empty when the row is valid.</summary>
        string Error { get; }
    }
}
