using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Contents.Domain.Policies;

public static class ContentItemRules
{
    public static EntityError StructuredMutationConflict() =>
        EntityError.Conflict("Structured entries must be changed through content authoring.");

    public static IReadOnlyList<EntityError> Validate(ContentItemWriteDto item)
    {
        ArgumentNullException.ThrowIfNull(item);

        List<EntityError> errors = [];
        if (string.IsNullOrWhiteSpace(item.Slug))
        {
            errors.Add(EntityError.Validation("Slug is required.", nameof(item.Slug)));
        }

        if (string.IsNullOrWhiteSpace(item.CanonicalPath))
        {
            errors.Add(EntityError.Validation("Canonical path is required.", nameof(item.CanonicalPath)));
        }

        if (string.IsNullOrWhiteSpace(item.Title))
        {
            errors.Add(EntityError.Validation("Title is required.", nameof(item.Title)));
        }

        if (string.IsNullOrWhiteSpace(item.Locale))
        {
            errors.Add(EntityError.Validation("Locale is required.", nameof(item.Locale)));
        }

        if (item.Status == ContentStatus.Scheduled && item.PublishedAt is null)
        {
            errors.Add(EntityError.Validation("Scheduled content requires PublishedAt.", nameof(item.PublishedAt)));
        }

        return errors;
    }

    public static bool ShouldCreateRevisionOnCreate(ContentStatus status) => IsRevisionStatus(status);

    public static bool ShouldCreateRevisionOnUpdate(ContentStatus? previousStatus, ContentStatus nextStatus) =>
        previousStatus is not null && previousStatus != nextStatus && IsRevisionStatus(nextStatus);

    private static bool IsRevisionStatus(ContentStatus status) =>
        status is ContentStatus.Published or ContentStatus.Archived;
}
