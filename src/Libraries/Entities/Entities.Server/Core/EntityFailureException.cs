namespace Vitarish.Entities.Server.Core;
/// <summary>
/// Represents a domain specific failure during an entity operation.
/// </summary>
public class EntityFailureException(EntityError error, Exception? innerException = null) : Exception(error.Message, innerException)
{
    public EntityError Error { get; } = error ?? throw new ArgumentNullException(nameof(error));
}
