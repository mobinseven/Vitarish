namespace Vitarish.Sea.Domain.Entities;

public class Event : Entity
{
    public Guid ProjectId { get; private set; }
    public Guid ProcedureId { get; private set; }

    public string Title
    {
        get;
        set => SetField(ref field, value, nameof(Title));
    } = string.Empty;

    public string Description
    {
        get;
        set => SetField(ref field, value, nameof(Description));
    } = string.Empty;

    public Guid? PreviousEventId
    {
        get;
        set => SetField(ref field, value, nameof(PreviousEventId));
    }

    public Guid CommittedBy
    {
        get;
        private set => SetField(ref field, value, nameof(CommittedBy));
    } = default;

    public DateTime? CommittedAt
    {
        get;
        private set => SetField(ref field, value, nameof(CommittedAt));
    }


    public EventStats Stats { get; set; } = new();

    public Event(Guid projectId, Guid procedureId, Guid? previousEventId)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        ProcedureId = procedureId;
        PreviousEventId = previousEventId;
    }

    public void Commit(Guid userId, DateTime commitTime)
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
