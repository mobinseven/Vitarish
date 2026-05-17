using System.ComponentModel.DataAnnotations;

namespace Vitarish.Users.Domain.Entities;

public class TemporaryAutoAuthLink
{
    private TemporaryAutoAuthLink()
    {
    }

    [Key]
    public Guid Id { get; private set; }

    [Required]
    [MaxLength(128)]
    public string CodeHash { get; private set; } = string.Empty;

    [Required]
    [MaxLength(450)]
    public string UserId { get; private set; } = string.Empty;

    public User? User { get; set; }

    [Required]
    [MaxLength(2048)]
    public string DestinationPath { get; private set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Purpose { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    [MaxLength(200)]
    public string? CreatedByDeviceHint { get; private set; }

    [MaxLength(45)]
    public string? ConsumedByIp { get; private set; }

    [MaxLength(512)]
    public string? ConsumedByUserAgent { get; private set; }

    public static TemporaryAutoAuthLink Create(
        Guid id,
        string codeHash,
        string userId,
        string destinationPath,
        string purpose,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        string? createdByDeviceHint = null)
    {
        ValidateRequired(codeHash, nameof(codeHash));
        ValidateRequired(userId, nameof(userId));
        ValidateRequired(destinationPath, nameof(destinationPath));
        ValidateRequired(purpose, nameof(purpose));
        if (expiresAt <= createdAt)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAt), "ExpiresAt must be later than CreatedAt.");
        }

        return new TemporaryAutoAuthLink
        {
            Id = id,
            CodeHash = codeHash,
            UserId = userId,
            DestinationPath = destinationPath,
            Purpose = purpose,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
            CreatedByDeviceHint = createdByDeviceHint
        };
    }

    public void MarkConsumed(DateTimeOffset consumedAt, string? consumedByIp, string? consumedByUserAgent)
    {
        if (ConsumedAt.HasValue)
        {
            throw new InvalidOperationException("Link has already been consumed.");
        }

        if (consumedAt < CreatedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(consumedAt), "ConsumedAt cannot be earlier than CreatedAt.");
        }

        ConsumedAt = consumedAt;
        ConsumedByIp = consumedByIp;
        ConsumedByUserAgent = consumedByUserAgent;
    }

    private static void ValidateRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
