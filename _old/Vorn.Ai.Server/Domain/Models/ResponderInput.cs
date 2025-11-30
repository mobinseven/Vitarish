namespace Vorn.Ai.Server.Domain.Models;

public enum ResponderInputImageDetail
{
    Low,
    High,
    Auto
}

public enum ResponderInputType
{
    Text,
    Image,
    File,
    McpToolCallApproval
}

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
    public static ResponderInput TextInput(string text)
    {
        return new(ResponderInputType.Text, Text: text);
    }

    public static ResponderInput ImageInput(string imageUrl, ResponderInputImageDetail? detail = null)
    {
        return new(ResponderInputType.Image, ImageUrl: imageUrl, Detail: detail);
    }

    public static ResponderInput FileInput(string? fileId = null, string? fileData = null, string? filename = null)
    {
        return new(ResponderInputType.File, FileId: fileId, FileData: fileData, Filename: filename);
    }

    public static ResponderInput McpToolCallApprovalInput(string approvalId)
    {
        return new(ResponderInputType.McpToolCallApproval, ApprovalId: approvalId);
    }
}

