using System.Text.Json;

namespace Vitarish.Clinic.Infrastructure.Data;

internal sealed class SurgeryView
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string ObjectType { get; set; } = string.Empty;
    public Guid ConcurrencyToken { get; set; }
    public JsonDocument Data { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
