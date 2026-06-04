using System.ComponentModel.DataAnnotations.Schema;

namespace Vitarish.Sea.Domain.Models;

public sealed class ProjectStats
{
    public Guid ProjectId { get; init; }
    public int Procedures { get; init; }
    public int Events { get; init; }
    public int CommittedEvents { get; init; }
    public int EventItems { get; init; }
    public int NewItems { get; init; }
    public bool Unread { get; init; }
    public DateTime? LastUpdate { get; init; }

    [NotMapped]
    public ProgressState State => Progress.FromCounts(CommittedEvents, Events).State;
}
