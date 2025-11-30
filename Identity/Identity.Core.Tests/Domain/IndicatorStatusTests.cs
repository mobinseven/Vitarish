using Vitarish.Identity.Core.Domain.Enums;

namespace Vitarish.Identity.Core.Tests.Domain;

/// <summary>
/// Tests for indicator status values and transitions.
/// </summary>
public class IndicatorStatusTests
{
    [Fact]
    public void IndicatorStatus_HasCorrectValues()
    {
        Assert.Equal(1, (int)IndicatorStatus.PendingVerification);
        Assert.Equal(2, (int)IndicatorStatus.Verified);
        Assert.Equal(3, (int)IndicatorStatus.Active);
        Assert.Equal(4, (int)IndicatorStatus.Disabled);
    }

    [Fact]
    public void IndicatorMethod_HasCorrectValues()
    {
        Assert.Equal(1, (int)IndicatorMethod.Hostname);
        Assert.Equal(2, (int)IndicatorMethod.Subdomain);
    }

    [Fact]
    public void IndicatorVerificationMethod_HasCorrectValues()
    {
        Assert.Equal(0, (int)IndicatorVerificationMethod.Manual);
        Assert.Equal(1, (int)IndicatorVerificationMethod.DnsTxt);
    }

    [Theory]
    [InlineData(IndicatorStatus.PendingVerification, IndicatorStatus.Verified, true)]
    [InlineData(IndicatorStatus.Verified, IndicatorStatus.Active, true)]
    [InlineData(IndicatorStatus.Active, IndicatorStatus.Disabled, true)]
    [InlineData(IndicatorStatus.Verified, IndicatorStatus.Disabled, true)]
    [InlineData(IndicatorStatus.PendingVerification, IndicatorStatus.Disabled, true)]
    public void IndicatorStatus_ValidTransitions(IndicatorStatus from, IndicatorStatus to, bool expectedValid)
    {
        // This is a logical test documenting valid state transitions
        // The actual validation logic would be in the application layer
        var isValid = IsValidTransition(from, to);
        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData(IndicatorStatus.Active, IndicatorStatus.PendingVerification, false)]
    [InlineData(IndicatorStatus.Active, IndicatorStatus.Verified, false)]
    [InlineData(IndicatorStatus.Verified, IndicatorStatus.PendingVerification, false)]
    [InlineData(IndicatorStatus.Disabled, IndicatorStatus.Active, false)]
    public void IndicatorStatus_InvalidTransitions(IndicatorStatus from, IndicatorStatus to, bool expectedValid)
    {
        // This is a logical test documenting invalid state transitions
        var isValid = IsValidTransition(from, to);
        Assert.Equal(expectedValid, isValid);
    }

    /// <summary>
    /// Helper method to validate status transitions.
    /// This mirrors the logic that should be in the application layer.
    /// </summary>
    private static bool IsValidTransition(IndicatorStatus from, IndicatorStatus to)
    {
        return (from, to) switch
        {
            // From PendingVerification
            (IndicatorStatus.PendingVerification, IndicatorStatus.Verified) => true,
            (IndicatorStatus.PendingVerification, IndicatorStatus.Disabled) => true,
            
            // From Verified
            (IndicatorStatus.Verified, IndicatorStatus.Active) => true,
            (IndicatorStatus.Verified, IndicatorStatus.Disabled) => true,
            
            // From Active
            (IndicatorStatus.Active, IndicatorStatus.Disabled) => true,
            
            // All other transitions are invalid
            _ => false
        };
    }
}
