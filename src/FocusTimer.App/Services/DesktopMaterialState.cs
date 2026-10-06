namespace FocusTimer.App.Services
{
    /// <summary>
    /// The material a Settings or Worklog window is currently painted with.
    /// </summary>
    public enum DesktopMaterialState
    {
        /// <summary>An opaque, theme-colored surface (effects unavailable, reduced transparency, or High Contrast).</summary>
        SolidFallback,

        /// <summary>A near-solid tint over a verified native blur.</summary>
        DenseFrost,
    }
}
