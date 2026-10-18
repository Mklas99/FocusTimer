namespace FocusTimer.App.Services
{
    /// <summary>The material actually selected for the widget's single shell.</summary>
    public enum WidgetMaterialState
    {
        /// <summary>Clear desktop with the requested tint.</summary>
        Clear,

        /// <summary>Qualified native frost with the requested tint.</summary>
        Frosted,

        /// <summary>Explicitly requested opaque shell.</summary>
        Solid,

        /// <summary>Opaque shell because the requested effect is unavailable.</summary>
        SolidFallback,
    }
}
