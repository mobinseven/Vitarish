using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Medical.Application.Abstractions.Prescriptions;
using Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;
using Vitarish.Modules.Base.Core.Platform.Errors;

namespace Medical.Tests.Infrastructure.Providers.Prescriptions.Tamin;

public sealed class TaminInsuranceAdapterReferenceDataTests
{
    [Fact]
    public async Task Reference_fetch_without_a_credential_asks_for_a_connected_tamin_account()
    {
        // The session store, gateway and database are not reached when the request has no credential.
        TaminInsuranceAdapter adapter = new(
            null!,
            null!,
            new TaminPrescriptionMapper(),
            new TaminDateFormatter(),
            Mock.Of<IOptionsMonitor<TaminOptions>>(),
            null!,
            new TaminErrorMapper(),
            NullLogger<TaminInsuranceAdapter>.Instance);

        AdapterResult<ReferenceDataResponse> result = await adapter.FetchReferenceDataAsync(
            new ReferenceDataRequest { CodeSystem = "drug" },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Category.Should().Be(ErrorCategory.Validation);
    }
}
