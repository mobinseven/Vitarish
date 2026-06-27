namespace Vitarish.Modules.Base.BackgroundJobs;

/// <summary>
/// Applies request identity values for background work running outside HTTP.
/// </summary>
public interface IBackgroundRequestContextWriter
{
    /// <summary>
    /// Applies the supplied request identity values to the active execution context.
    /// </summary>
    void SetContext(BackgroundRequestContext context);
}
