namespace Vitarish.Cms.Domain.Entities;

public class ContentComment : Entity
{
    private Guid _contentId;
    public Guid ContentId
    {
        get => _contentId;
        private set => SetField(ref _contentId, value, nameof(ContentId));
    }

    private Guid? _parentCommentId;
    public Guid? ParentCommentId
    {
        get => _parentCommentId;
        private set => SetField(ref _parentCommentId, value, nameof(ParentCommentId));
    }

    private string _authorName = string.Empty;
    public string AuthorName
    {
        get => _authorName;
        private set => SetField(ref _authorName, value, nameof(AuthorName));
    }

    private string? _authorId;
    public string? AuthorId
    {
        get => _authorId;
        private set => SetField(ref _authorId, value, nameof(AuthorId));
    }

    private string _body = string.Empty;
    public string Body
    {
        get => _body;
        private set => SetField(ref _body, value, nameof(Body));
    }

    private bool _isApproved;
    public bool IsApproved
    {
        get => _isApproved;
        private set => SetField(ref _isApproved, value, nameof(IsApproved));
    }

    public ContentItem? Content { get; set; }
    public ContentComment? ParentComment { get; set; }
    public List<ContentComment> Replies { get; } = new();

    public ContentComment() { }

    public ContentComment(Guid contentId, string authorName, string body, Guid? parentCommentId = null, string? authorId = null)
    {
        Id = Guid.NewGuid();
        _contentId = contentId;
        _parentCommentId = parentCommentId;
        _authorName = authorName;
        _body = body;
        _authorId = authorId;
    }

    public void UpdateBody(string body) => Body = body;

    public void Approve() => IsApproved = true;

    public void Reject() => IsApproved = false;
}
