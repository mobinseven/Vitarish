using FluentAssertions;
using Tamin.Integration.Auth;
using Tamin.Integration.Mapping;
using Vitarish.Medical.Application.Abstractions.Prescriptions;
using Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;
using Vitarish.Modules.Base.Core.Platform.Errors;

namespace Medical.Tests.Infrastructure.Providers.Prescriptions.Tamin;

public sealed class TaminErrorMapperTests
{
    private readonly TaminErrorMapper _mapper = new();

    [Fact]
    public void Rejected_token_refresh_asks_the_doctor_to_log_in_again()
    {
        IntegrationError error = _mapper.MapError(new ProviderErrorContext
        {
            Exception = new TaminReauthorizationRequiredException("Doctor authorization must be completed again."),
            ProviderErrorCode = "reauthorization_required"
        });

        error.Category.Should().Be(ErrorCategory.Authentication);
        error.IsRetryable.Should().BeFalse();
    }

    [Fact]
    public void Invalid_request_field_is_a_validation_error()
    {
        IntegrationError error = _mapper.MapError(new ProviderErrorContext
        {
            Exception = new TaminRequestValidationException("srvType", "'99' is not a documented value")
        });

        error.Category.Should().Be(ErrorCategory.Validation);
        error.IsRetryable.Should().BeFalse();
    }
}
