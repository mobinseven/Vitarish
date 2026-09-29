namespace Vitarish.Files.Domain.Models;

/// <summary>
/// Files-owned policy projection requested by an owning feature.
/// </summary>
public enum FileTransferPurpose
{
    Attachment = 0,
    KnowledgeImport = 1,
    Avatar = 2,
    AudioRecording = 3
}
