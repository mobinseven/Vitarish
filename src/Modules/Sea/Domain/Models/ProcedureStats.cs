
using System.ComponentModel.DataAnnotations.Schema;

namespace Vitarish.Sea.Domain.Models;

public sealed class ProcedureStats
{
    public Guid ProcedureId { get; init; }
    public int Events { get; init; }
    public int CommittedEvents { get; init; }
    public int EventItems { get; init; }
    public int NewItems { get; init; }
    public bool Unread { get; init; }
    public DateTimeOffset? LastUpdate { get; init; }
    public Procedure Procedure { get; init; } = null!;

    [NotMapped]
    public ProgressState State => Progress.FromCounts(CommittedEvents, Events).State;
}
