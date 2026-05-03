namespace Vitarish.Sea.Domain.Entities;

public class ProjectUser : Entity
{
    public Guid ProjectId
    {
        get;
        private set => SetField(ref field, value, nameof(ProjectId));
    } = Guid.Empty;
    public string UserId
    {
        get;
        private set => SetField(ref field, value, nameof(UserId));
    } = string.Empty;

    public ProjectUser() { }

    public ProjectUser(Guid projectId, string userId)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        UserId = userId;
    }
}
