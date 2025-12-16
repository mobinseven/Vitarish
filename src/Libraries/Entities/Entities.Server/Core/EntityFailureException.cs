namespace Vitarish.Entities.Server.Core;
/// <summary>
/// Represents a domain specific failure during an entity operation.
/// </summary>
public class EntityFailureException : Exception
{
    public EntityFailureException(EntityError error, Exception? innerException = null)
        : base(error.Message, innerException)
    {
        Error = error ?? throw new ArgumentNullException(nameof(error));
    }

    public EntityError Error { get; }
}
