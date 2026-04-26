namespace Vitarish.Sea.Domain.Entities;

public class Attachment : EventItem
{
    public string Content
    {
        get;
        set => SetField(ref field, value, nameof(Content));
    } = string.Empty;

    public string ContentType
    {
        get;
        set => SetField(ref field, value, nameof(ContentType));
    } = string.Empty;

    public string? ContentThumbnail
    {
        get;
        set => SetField(ref field, value, nameof(ContentThumbnail));
    }

    public long Size
    {
        get;
        set => SetField(ref field, value, nameof(Size));
    }
}
