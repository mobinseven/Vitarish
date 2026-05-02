namespace Vitarish.Sea.Domain.Entities;

public class ProjectUser : Entity
{
    private Guid _projectId = Guid.Empty;
    public Guid ProjectId
    {
        get => _projectId;
        private set => SetField(ref _projectId, value, nameof(ProjectId));
    }

    private string _userId = string.Empty;
    public string UserId
    {
        get => _userId;
        private set => SetField(ref _userId, value, nameof(UserId));
    }

    public ProjectUser() { }

    public ProjectUser(Guid projectId, string userId)
    {
        Id = Guid.NewGuid();
        _projectId = projectId;
        _userId = userId;
    }
}
