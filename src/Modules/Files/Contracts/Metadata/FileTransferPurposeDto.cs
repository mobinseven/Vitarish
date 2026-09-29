namespace Vitarish.Files.Contracts.Metadata;

/// <summary>Serialization-safe projection of the Files-owned transfer purpose.</summary>
public enum FileTransferPurposeDto
{
    Attachment = 0,
    KnowledgeImport = 1,
    Avatar = 2,
    AudioRecording = 3
}
