namespace Vitarish.Sea.Domain.Models;

public sealed record Progress(int Completed, int Total, double Percentage, ProgressState State)
{
    public static Progress FromCounts(int completed, int total)
    {
        double percentage = total == 0 ? 0d : (double)completed / total;
        ProgressState state = percentage switch
        {
            0 => ProgressState.NotStarted,
            <= 0.25 => ProgressState.Quarter,
            <= 0.5 => ProgressState.Half,
            <= 0.75 => ProgressState.ThreeQuarters,
            < 1 => ProgressState.InProgress,
            _ => ProgressState.Completed
        };
        return new(completed, total, percentage, state);
    }
}
