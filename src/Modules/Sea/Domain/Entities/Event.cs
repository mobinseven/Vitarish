namespace Vitarish.Sea.Domain.Entities;

public class Event : Entity
{
    private string _title = string.Empty;
    private string _description = string.Empty;
    private Guid? _previousEventId;
    private Guid _reporterUserId = default;
    private DateTimeOffset? _committedAt;

    public Guid ProjectId { get; private set; }
    public Guid ProcedureId { get; private set; }

    public string Title
    {
        get => _title;
        set => SetField(ref _title, value, nameof(Title));
    }

    public string Description
    {
        get => _description;
        set => SetField(ref _description, value, nameof(Description));
    }

    public Guid? PreviousEventId
    {
        get => _previousEventId;
        set => SetField(ref _previousEventId, value, nameof(PreviousEventId));
    }

    public Guid CommittedBy
    {
        get => _reporterUserId;
        private set => SetField(ref _reporterUserId, value, nameof(CommittedBy));
    }

    public DateTimeOffset? CommittedAt
    {
        get => _committedAt;
        private set => SetField(ref _committedAt, value, nameof(CommittedAt));
    }


    public EventStats Stats { get; set; } = new();

    public Event(Guid projectId, Guid procedureId, Guid? previousEventId)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        ProcedureId = procedureId;
        PreviousEventId = previousEventId;
    }

    public void Commit(Guid userId, DateTimeOffset commitTime)
    {
        CommittedBy = userId;
        CommittedAt = commitTime;
    }

    public void Uncommit()
    {
        CommittedBy = default;
        CommittedAt = null;
    }

}
