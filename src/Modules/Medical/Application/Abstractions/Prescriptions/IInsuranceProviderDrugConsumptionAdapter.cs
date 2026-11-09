namespace Vitarish.Medical.Application.Abstractions.Prescriptions;

/// <summary>
/// Optional capability for providers that require consumption values (amount, time, route, method)
/// chosen from lists that depend on the drug's dosage form.
/// </summary>
public interface IInsuranceProviderDrugConsumptionAdapter
{
    Task<AdapterResult<DrugConsumptionOptions>> GetDrugConsumptionOptionsAsync(
        DrugConsumptionOptionsRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record DrugConsumptionOptionsRequest
{
    public required Guid DoctorId { get; init; }
    public required Guid ClinicId { get; init; }
    public required string FormCode { get; init; }
}

public sealed record DrugConsumptionOption(string Id, string Name, string? NameLatin);

/// <summary>A consumption unit with the amounts that are valid for it.</summary>
public sealed record DrugConsumptionUnit(string Id, string Name, string? NameLatin, IReadOnlyList<DrugConsumptionOption> Amounts);

public sealed record DrugConsumptionOptions
{
    public required IReadOnlyList<DrugConsumptionUnit> Units { get; init; }
    public required IReadOnlyList<DrugConsumptionOption> Instructions { get; init; }
    public required IReadOnlyList<DrugConsumptionOption> Usages { get; init; }
    public required IReadOnlyList<DrugConsumptionOption> Methods { get; init; }
}
