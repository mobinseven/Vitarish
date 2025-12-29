namespace Vitarish.Parties.Tests;

using FluentValidation;
using Vitarish.Parties.Application.Commands;
using Vitarish.Parties.Domain.Enums;

public class CommandValidationTests
{
    [Fact]
    public void CreateTenantValidator_RejectsMissingFields()
    {
        var validator = new CreateTenantValidator();
        var command = new CreateTenant(Guid.NewGuid(), string.Empty, string.Empty);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTenant.Name));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTenant.ClientId));
    }

    [Fact]
    public void CreateTenantValidator_AllowsValidFields()
    {
        var validator = new CreateTenantValidator();
        var command = new CreateTenant(Guid.NewGuid(), "Tenant", "client-id");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdateTenantValidator_RejectsMissingFields()
    {
        var validator = new UpdateTenantValidator();
        var command = new UpdateTenant(Guid.NewGuid(), Guid.NewGuid(), string.Empty, string.Empty);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateTenant.Name));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateTenant.ClientId));
    }

    [Fact]
    public void CreateOwnerValidator_RejectsMissingName()
    {
        var validator = new CreateOwnerValidator();
        var command = new CreateOwner(string.Empty);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateOwner.Name));
    }

    [Fact]
    public void UpdateOwnerValidator_RejectsMissingName()
    {
        var validator = new UpdateOwnerValidator();
        var command = new UpdateOwner(Guid.NewGuid(), string.Empty);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateOwner.Name));
    }

    [Fact]
    public void AddTenantIndicatorValidator_RejectsInvalidHostname()
    {
        var validator = new AddTenantIndicatorValidator();
        var command = new AddTenantIndicator(Guid.NewGuid(), IndicatorMethod.Hostname, "not a host");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddTenantIndicator.Value));
    }

    [Fact]
    public void AddTenantIndicatorValidator_RejectsOutOfRangeMethod()
    {
        var validator = new AddTenantIndicatorValidator();
        var command = new AddTenantIndicator(Guid.NewGuid(), (IndicatorMethod)99, "example.com");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddTenantIndicator.Method));
    }

    [Fact]
    public void AddTenantIndicatorValidator_RejectsTooLongHostname()
    {
        var validator = new AddTenantIndicatorValidator();
        var hostname = new string('a', 254);
        var command = new AddTenantIndicator(Guid.NewGuid(), IndicatorMethod.Hostname, hostname);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddTenantIndicator.Value));
    }

    [Fact]
    public void AddTenantIndicatorValidator_AllowsValidHostname()
    {
        var validator = new AddTenantIndicatorValidator();
        var command = new AddTenantIndicator(Guid.NewGuid(), IndicatorMethod.Hostname, "example.com");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
