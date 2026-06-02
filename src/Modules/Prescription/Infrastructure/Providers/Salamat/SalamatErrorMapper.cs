using Vitarish.Modules.Base.Platform.Errors;
using Vitarish.Prescription.Application.Abstractions;

namespace Vitarish.Prescription.Infrastructure.Providers.Salamat;

/// <summary>
/// Maps Salamat (Ministry of Health e-Prescription) errors to the canonical error taxonomy.
/// </summary>
public sealed class SalamatErrorMapper : IProviderErrorMapper
{
    public string ProviderKey => SalamatInsuranceAdapter.ProviderKeyConst;

    public IntegrationError MapError(ProviderErrorContext context) => context.HttpStatusCode.HasValue
            ? context.HttpStatusCode.Value switch
            {
                401 or 403 => new IntegrationError
                {
                    Category = ErrorCategory.Authentication,
                    UserMessage = "نشست سامانه سلامت منقضی شده. لطفاً دوباره وارد شوید.",
                    TechnicalDetail = context.ProviderErrorMessage,
                    ProviderErrorCode = context.ProviderErrorCode,
                    ProviderErrorMessage = context.ProviderErrorMessage,
                    IsRetryable = false
                },
                400 => new IntegrationError
                {
                    Category = ErrorCategory.Validation,
                    UserMessage = "اطلاعات ارسالی به سامانه سلامت نامعتبر است.",
                    TechnicalDetail = context.ProviderErrorMessage,
                    ProviderErrorCode = context.ProviderErrorCode,
                    ProviderErrorMessage = context.ProviderErrorMessage,
                    IsRetryable = false
                },
                409 => new IntegrationError
                {
                    Category = ErrorCategory.DuplicateRisk,
                    UserMessage = "احتمال ثبت تکراری نسخه در سامانه سلامت وجود دارد.",
                    TechnicalDetail = context.ProviderErrorMessage,
                    ProviderErrorCode = context.ProviderErrorCode,
                    ProviderErrorMessage = context.ProviderErrorMessage,
                    IsRetryable = false
                },
                429 => new IntegrationError
                {
                    Category = ErrorCategory.RateLimit,
                    UserMessage = "تعداد درخواست‌ها به سامانه سلامت بیش از حد مجاز است.",
                    TechnicalDetail = context.ProviderErrorMessage,
                    ProviderErrorCode = context.ProviderErrorCode,
                    ProviderErrorMessage = context.ProviderErrorMessage,
                    IsRetryable = true
                },
                408 or 504 => new IntegrationError
                {
                    Category = ErrorCategory.Timeout,
                    UserMessage = "سامانه سلامت پاسخگو نیست. لطفاً دوباره تلاش کنید.",
                    TechnicalDetail = context.ProviderErrorMessage,
                    ProviderErrorCode = context.ProviderErrorCode,
                    ProviderErrorMessage = context.ProviderErrorMessage,
                    IsRetryable = true
                },
                502 or 503 => new IntegrationError
                {
                    Category = ErrorCategory.Outage,
                    UserMessage = "سامانه سلامت در دسترس نیست.",
                    TechnicalDetail = context.ProviderErrorMessage,
                    ProviderErrorCode = context.ProviderErrorCode,
                    ProviderErrorMessage = context.ProviderErrorMessage,
                    IsRetryable = true
                },
                _ => new IntegrationError
                {
                    Category = ErrorCategory.Unknown,
                    UserMessage = "خطای ناشناخته در ارتباط با سامانه سلامت رخ داد.",
                    TechnicalDetail = context.ProviderErrorMessage,
                    ProviderErrorCode = context.ProviderErrorCode,
                    ProviderErrorMessage = context.ProviderErrorMessage,
                    IsRetryable = false
                }
            }
            : context.Exception is TimeoutException or TaskCanceledException
            ? new IntegrationError
            {
                Category = ErrorCategory.Timeout,
                UserMessage = "سامانه سلامت پاسخگو نیست. لطفاً دوباره تلاش کنید.",
                TechnicalDetail = context.Exception.Message,
                IsRetryable = true
            }
            : context.Exception is HttpRequestException
            ? new IntegrationError
            {
                Category = ErrorCategory.Outage,
                UserMessage = "ارتباط با سامانه سلامت برقرار نشد.",
                TechnicalDetail = context.Exception.Message,
                IsRetryable = true
            }
            : new IntegrationError
            {
                Category = ErrorCategory.Unknown,
                UserMessage = "خطای ناشناخته رخ داد.",
                TechnicalDetail = context.Exception?.Message ?? context.ProviderErrorMessage,
                ProviderErrorCode = context.ProviderErrorCode,
                ProviderErrorMessage = context.ProviderErrorMessage,
                IsRetryable = false
            };
}
