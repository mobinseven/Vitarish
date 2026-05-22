public static class ShiftExtensions
{
    public static Color ToColor(this ShiftStatus Status) => Status switch
    {
        ShiftStatus.Draft => Color.Default,
        ShiftStatus.Active => Color.Success,
        ShiftStatus.Paused => Color.Warning,
        ShiftStatus.Closed => Color.Dark,
        ShiftStatus.Cancelled => Color.Error,
        ShiftStatus.Archived => Color.Dark,
        _ => Color.Default
    };
}
