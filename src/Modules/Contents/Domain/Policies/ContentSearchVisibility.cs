namespace Vitarish.Contents.Domain.Policies;

public static class ContentSearchVisibility
{
    public static bool IsSearchable(ContentStatus status) => status == ContentStatus.Published;
}
