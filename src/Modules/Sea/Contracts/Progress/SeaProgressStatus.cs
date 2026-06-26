namespace Vitarish.Sea.Contracts.Progress;

public sealed record SeaProgressStatus(ProgressState State, int? Completed, int? Total, string? Message);
