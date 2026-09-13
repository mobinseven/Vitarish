using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Vitarish.Sea.Domain.Entities;

namespace Vitarish.Sea.Domain.Models;

/// <summary>
/// Defines the Sea-owned canonical representation of an attachment's sensitive content and metadata.
/// </summary>
public static class AttachmentIntegrityProjection
{
    public const int Version = 1;

    public static string ComputeDigest(Attachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);

        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendInt32(hash, Version);
        hash.AppendData(attachment.Id.ToByteArray());
        hash.AppendData(attachment.EventId.ToByteArray());
        AppendInt32(hash, (int)attachment.Kind);
        AppendString(hash, attachment.Content);
        AppendString(hash, attachment.ContentType);
        AppendString(hash, attachment.ContentThumbnail);
        AppendInt64(hash, attachment.Size);
        AppendString(hash, attachment.FileName);
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static void AppendString(IncrementalHash hash, string? value)
    {
        if (value is null)
        {
            AppendInt32(hash, -1);
            return;
        }

        int byteCount = Encoding.UTF8.GetByteCount(value);
        AppendInt32(hash, byteCount);
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        try
        {
            hash.AppendData(bytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    private static void AppendInt32(IncrementalHash hash, int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        hash.AppendData(bytes);
    }

    private static void AppendInt64(IncrementalHash hash, long value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(bytes, value);
        hash.AppendData(bytes);
    }
}
