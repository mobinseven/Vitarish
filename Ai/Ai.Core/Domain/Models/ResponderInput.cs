namespace Vitarish.Ai.Core.Domain.Models;

/// <summary>
/// Type of responder input.
/// </summary>
public enum ResponderInputType
{
    /// <summary>Text input.</summary>
    Text,
    /// <summary>Image input.</summary>
    Image,
    /// <summary>File input.</summary>
    File,
    /// <summary>MCP tool call approval input.</summary>
    McpToolCallApproval
}

/// <summary>
/// Image detail level for responder input.
/// </summary>
public enum ResponderInputImageDetail
{
    /// <summary>Low detail.</summary>
    Low,
    /// <summary>High detail.</summary>
    High,
    /// <summary>Auto-detect detail level.</summary>
    Auto
}

/// <summary>
/// Input data for the AI responder.
/// </summary>
/// <param name="Type">The type of input.</param>
/// <param name="Text">Text content for text inputs.</param>
/// <param name="ImageUrl">URL for image inputs.</param>
/// <param name="Detail">Detail level for image inputs.</param>
/// <param name="FileId">File ID for file inputs.</param>
/// <param name="FileData">File data for file inputs.</param>
/// <param name="Filename">Filename for file inputs.</param>
/// <param name="ApprovalId">Approval ID for MCP tool call approval inputs.</param>
public sealed record ResponderInput(
    ResponderInputType Type,
    string? Text = null,
    string? ImageUrl = null,
    ResponderInputImageDetail? Detail = null,
    string? FileId = null,
    string? FileData = null,
    string? Filename = null,
    string? ApprovalId = null
)
{
    /// <summary>
    /// Creates a text input.
    /// </summary>
    /// <param name="text">The text content.</param>
    /// <returns>A new ResponderInput for text.</returns>
    public static ResponderInput TextInput(string text)
        => new(ResponderInputType.Text, Text: text);

    /// <summary>
    /// Creates an image input.
    /// </summary>
    /// <param name="imageUrl">The image URL.</param>
    /// <param name="detail">Optional detail level.</param>
    /// <returns>A new ResponderInput for image.</returns>
    public static ResponderInput ImageInput(string imageUrl, ResponderInputImageDetail? detail = null)
        => new(ResponderInputType.Image, ImageUrl: imageUrl, Detail: detail);

    /// <summary>
    /// Creates a file input.
    /// </summary>
    /// <param name="fileId">Optional file ID.</param>
    /// <param name="fileData">Optional file data.</param>
    /// <param name="filename">Optional filename.</param>
    /// <returns>A new ResponderInput for file.</returns>
    public static ResponderInput FileInput(string? fileId = null, string? fileData = null, string? filename = null)
        => new(ResponderInputType.File, FileId: fileId, FileData: fileData, Filename: filename);

    /// <summary>
    /// Creates an MCP tool call approval input.
    /// </summary>
    /// <param name="approvalId">The approval ID.</param>
    /// <returns>A new ResponderInput for MCP tool call approval.</returns>
    public static ResponderInput McpToolCallApprovalInput(string approvalId)
        => new(ResponderInputType.McpToolCallApproval, ApprovalId: approvalId);
}
