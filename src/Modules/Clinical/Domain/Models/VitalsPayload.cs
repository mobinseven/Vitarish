namespace Vitarish.Clinical.Domain.Models;

public sealed record VitalsPayload
{
    public decimal? SystolicBp { get; init; }
    public decimal? DiastolicBp { get; init; }
    public decimal? PulseRate { get; init; }
    public decimal? Temperature { get; init; }
    public string? TemperatureUnit { get; init; } = "Cel";
    public decimal? WeightKg { get; init; }
    public decimal? HeightCm { get; init; }
    public decimal? SpO2Percent { get; init; }
    public decimal? RespiratoryRate { get; init; }
}
