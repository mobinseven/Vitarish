namespace Vitarish.Sea.Domain.Entities;

public class Attachment : EventItem
{
    private string _content = string.Empty;
    private string _contentType = string.Empty;
    private string? _contentThumbnail;
    private long _size;

    public string Content
    {
        get => _content;
        set => SetField(ref _content, value, nameof(Content));
    }

    public string ContentType
    {
        get => _contentType;
        set => SetField(ref _contentType, value, nameof(ContentType));
    }

    public string? ContentThumbnail
    {
        get => _contentThumbnail;
        set => SetField(ref _contentThumbnail, value, nameof(ContentThumbnail));
    }

    public long Size
    {
        get => _size;
        set => SetField(ref _size, value, nameof(Size));
    }
}
