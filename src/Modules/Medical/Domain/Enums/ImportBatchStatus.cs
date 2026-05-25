namespace Vitarish.Medical.Domain.Enums;

public enum ImportBatchStatus
{
    Pending,
    Validating,
    Imported,
    Failed,
    PartiallyImported,
    RolledBack
}
