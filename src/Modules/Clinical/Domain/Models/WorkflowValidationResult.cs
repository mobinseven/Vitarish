namespace Vitarish.Clinical.Domain.Models;

public sealed record WorkflowValidationResult
{
    public required WorkflowValidationSeverity Severity { get; init; }
    public required string Code { get; init; }
    public string? Field { get; init; }
    public required string Message { get; init; }

    public static WorkflowValidationResult Error(string code, string message, string? field = null) => new()
    {
        Severity = WorkflowValidationSeverity.Error,
        Code = code,
        Field = field,
        Message = message
    };

    public static WorkflowValidationResult Warning(string code, string message, string? field = null) => new()
    {
        Severity = WorkflowValidationSeverity.Warning,
        Code = code,
        Field = field,
        Message = message
    };

    public static WorkflowValidationResult Info(string code, string message, string? field = null) => new()
    {
        Severity = WorkflowValidationSeverity.Info,
        Code = code,
        Field = field,
        Message = message
    };
}
