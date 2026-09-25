namespace FocusTimer.App.ViewModels
{
    /// <summary>
    /// A grouping the user can choose in the summary view.
    /// </summary>
    /// <param name="Id">The grouping id used in summary requests.</param>
    /// <param name="DisplayName">The name shown to the user.</param>
    public sealed record GroupingOption(string Id, string DisplayName);
}
