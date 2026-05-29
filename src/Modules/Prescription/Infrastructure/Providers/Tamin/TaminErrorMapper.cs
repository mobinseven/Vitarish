using Vitarish.Modules.Base.Platform.Errors;
using Vitarish.Prescription.Application.Abstractions;

namespace Vitarish.Prescription.Infrastructure.Providers.Tamin;

/// <summary>
/// Maps Tamin-specific errors to the canonical error taxonomy.
/// </summary>
public sealed class TaminErrorMapper : IProviderErrorMapper
{
    public string ProviderKey => TaminInsuranceAdapter.ProviderKeyConst;

    public IntegrationError MapError(ProviderErrorContext context)
    {
        // Map by HTTP status first
        if (context.HttpStatusCode.HasValue)
        {
            return context.HttpStatusCode.Value switch
            {
                401 or 403 => new IntegrationError
                {
                    Category = ErrorCategory.Authentication,
                    UserMessage = "نشست تامین اجتماعی منقضی شده. لطفاً دوباره وارد شوید.",
                    TechnicalDetail = context.ProviderErrorMessage,
                    ProviderErrorCode = context.ProviderErrorCode,
                    ProviderErrorMessage = context.ProviderErrorMessage,
                    IsRetryable = false
                },
                400 => new IntegrationError
                {
                    Category = ErrorCategory.Validation,
                    UserMessage = "اطلاعات ارسالی به تامین اجتماعی نامعتبر است.",
                    TechnicalDetail = context.ProviderErrorMessage,
                    ProviderErrorCode = context.ProviderErrorCode,
                    ProviderErrorMessage = context.ProviderErrorMessage,
                    IsRetryable = false
                },
                409 => new IntegrationError
                {
                    Category = ErrorCategory.DuplicateRisk,
                    UserMessage = "احتمال ثبت تکراری نسخه وجود دارد. لطفاً وضعیت را بررسی کنید.",
                    TechnicalDetail = context.ProviderErrorMessage,
                    ProviderErrorCode = context.ProviderErrorCode,
                    ProviderErrorMessage = context.ProviderErrorMessage,
                    IsRetryable = false
                },
                429 => new IntegrationError
                {
                    Category = ErrorCategory.RateLimit,
                    UserMessage = "تعداد درخواست‌ها بیش از حد مجاز است. لطفاً کمی صبر کنید.",
                    TechnicalDetail = context.ProviderErrorMessage,
                    ProviderErrorCode = context.ProviderErrorCode,
                    ProviderErrorMessage = context.ProviderErrorMessage,
                    IsRetryable = true
                },
                408 or 504 => new IntegrationError
                {
                    Category = ErrorCategory.Timeout,
                    UserMessage = "سامانه تامین اجتماعی پاسخگو نیست. لطفاً دوباره تلاش کنید.",
                    TechnicalDetail = context.ProviderErrorMessage,
                    ProviderErrorCode = context.ProviderErrorCode,
                    ProviderErrorMessage = context.ProviderErrorMessage,
                    IsRetryable = true
                },
                502 or 503 => new IntegrationError
                {
                    Category = ErrorCategory.Outage,
                    UserMessage = "سامانه تامین اجتماعی در دسترس نیست.",
                    TechnicalDetail = context.ProviderErrorMessage,
                    ProviderErrorCode = context.ProviderErrorCode,
                    ProviderErrorMessage = context.ProviderErrorMessage,
                    IsRetryable = true
                },
                _ => new IntegrationError
                {
                    Category = ErrorCategory.Unknown,
                    UserMessage = "خطای ناشناخته در ارتباط با تامین اجتماعی رخ داد.",
                    TechnicalDetail = context.ProviderErrorMessage,
                    ProviderErrorCode = context.ProviderErrorCode,
                    ProviderErrorMessage = context.ProviderErrorMessage,
                    IsRetryable = false
                }
            };
        }

        // Map by exception type
        if (context.Exception is TimeoutException or TaskCanceledException)
        {
            return new IntegrationError
            {
                Category = ErrorCategory.Timeout,
                UserMessage = "سامانه تامین اجتماعی پاسخگو نیست. لطفاً دوباره تلاش کنید.",
                TechnicalDetail = context.Exception.Message,
                IsRetryable = true
            };
        }

        if (context.Exception is HttpRequestException)
        {
            return new IntegrationError
            {
                Category = ErrorCategory.Outage,
                UserMessage = "ارتباط با سامانه تامین اجتماعی برقرار نشد.",
                TechnicalDetail = context.Exception.Message,
                IsRetryable = true
            };
        }

        return new IntegrationError
        {
            Category = ErrorCategory.Unknown,
            UserMessage = "خطای ناشناخته رخ داد.",
            TechnicalDetail = context.Exception?.Message ?? context.ProviderErrorMessage,
            ProviderErrorCode = context.ProviderErrorCode,
            ProviderErrorMessage = context.ProviderErrorMessage,
            IsRetryable = false
        };
    }
}
