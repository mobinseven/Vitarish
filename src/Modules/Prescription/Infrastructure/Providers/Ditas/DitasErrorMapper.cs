using Vitarish.Modules.Base.Platform.Errors;
using Vitarish.Prescription.Application.Abstractions;

namespace Vitarish.Prescription.Infrastructure.Providers.Ditas;

/// <summary>
/// Maps DITAS (Armed Forces Insurance)-specific errors to the canonical error taxonomy.
/// </summary>
public sealed class DitasErrorMapper : IProviderErrorMapper
{
    public string ProviderKey => DitasInsuranceAdapter.ProviderKeyConst;

    public IntegrationError MapError(ProviderErrorContext context) => context.HttpStatusCode.HasValue
            ? context.HttpStatusCode.Value switch
            {
                401 or 403 => new IntegrationError
                {
                    Category = ErrorCategory.Authentication,
                    UserMessage = "نشست بیمه نیروهای مسلح منقضی شده. لطفاً دوباره وارد شوید.",
                    TechnicalDetail = context.ProviderErrorMessage,
                    ProviderErrorCode = context.ProviderErrorCode,
                    ProviderErrorMessage = context.ProviderErrorMessage,
                    IsRetryable = false
                },
                400 => new IntegrationError
                {
                    Category = ErrorCategory.Validation,
                    UserMessage = "اطلاعات ارسالی به سامانه نیروهای مسلح نامعتبر است.",
                    TechnicalDetail = context.ProviderErrorMessage,
                    ProviderErrorCode = context.ProviderErrorCode,
                    ProviderErrorMessage = context.ProviderErrorMessage,
                    IsRetryable = false
                },
                409 => new IntegrationError
                {
                    Category = ErrorCategory.DuplicateRisk,
                    UserMessage = "احتمال ثبت تکراری نسخه در سامانه نیروهای مسلح وجود دارد.",
                    TechnicalDetail = context.ProviderErrorMessage,
                    ProviderErrorCode = context.ProviderErrorCode,
                    ProviderErrorMessage = context.ProviderErrorMessage,
                    IsRetryable = false
                },
                429 => new IntegrationError
                {
                    Category = ErrorCategory.RateLimit,
                    UserMessage = "تعداد درخواست‌ها به سامانه نیروهای مسلح بیش از حد مجاز است.",
                    TechnicalDetail = context.ProviderErrorMessage,
                    ProviderErrorCode = context.ProviderErrorCode,
                    ProviderErrorMessage = context.ProviderErrorMessage,
                    IsRetryable = true
                },
                408 or 504 => new IntegrationError
                {
                    Category = ErrorCategory.Timeout,
                    UserMessage = "سامانه نیروهای مسلح پاسخگو نیست. لطفاً دوباره تلاش کنید.",
                    TechnicalDetail = context.ProviderErrorMessage,
                    ProviderErrorCode = context.ProviderErrorCode,
                    ProviderErrorMessage = context.ProviderErrorMessage,
                    IsRetryable = true
                },
                502 or 503 => new IntegrationError
                {
                    Category = ErrorCategory.Outage,
                    UserMessage = "سامانه نیروهای مسلح در دسترس نیست.",
                    TechnicalDetail = context.ProviderErrorMessage,
                    ProviderErrorCode = context.ProviderErrorCode,
                    ProviderErrorMessage = context.ProviderErrorMessage,
                    IsRetryable = true
                },
                _ => new IntegrationError
                {
                    Category = ErrorCategory.Unknown,
                    UserMessage = "خطای ناشناخته در ارتباط با سامانه نیروهای مسلح رخ داد.",
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
                UserMessage = "سامانه نیروهای مسلح پاسخگو نیست. لطفاً دوباره تلاش کنید.",
                TechnicalDetail = context.Exception.Message,
                IsRetryable = true
            }
            : context.Exception is HttpRequestException
            ? new IntegrationError
            {
                Category = ErrorCategory.Outage,
                UserMessage = "ارتباط با سامانه نیروهای مسلح برقرار نشد.",
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
