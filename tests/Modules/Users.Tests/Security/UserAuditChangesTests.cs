using FluentAssertions;
using Vitarish.Audit.Domain;
using Vitarish.Users.Application.Security;

namespace Vitarish.Users.Tests.Security;

public sealed class UserAuditChangesTests
{
    [Fact]
    public void Lists_only_the_fields_that_differ_with_their_previous_and_new_values()
    {
        IReadOnlyList<SecurityAuditChange> changes = UserAuditChanges.Between(
            ("old@example.test", "+989121234567", "Sara", "Ahmadi"),
            ("new@example.test", "+989121234567", "Sara", "Karimi"));

        changes.Should().Equal(
            new SecurityAuditChange(SecurityAuditChangeFields.Email, "old@example.test", "new@example.test"),
            new SecurityAuditChange(SecurityAuditChangeFields.LastName, "Ahmadi", "Karimi"));
    }

    [Fact]
    public void Treats_a_case_only_email_difference_as_a_change_but_blank_values_as_unchanged()
    {
        IReadOnlyList<SecurityAuditChange> changes = UserAuditChanges.Between(
            ("User@Example.test", null, " ", null),
            ("user@example.test", "", null, null));

        changes.Should().Equal(
            new SecurityAuditChange(SecurityAuditChangeFields.Email, "User@Example.test", "user@example.test"));
    }

    [Fact]
    public void Records_a_difference_past_the_display_bound_after_comparing_the_full_values()
    {
        string prefix = new('a', SecurityAuditChangeFields.MaximumValueLength);
        IReadOnlyList<SecurityAuditChange> changes = UserAuditChanges.Between(
            (null, null, prefix + "x", null),
            (null, null, prefix + "y", null));

        changes.Should().ContainSingle();
        changes[0].Field.Should().Be(SecurityAuditChangeFields.FirstName);
        changes[0].PreviousValue.Should().HaveLength(SecurityAuditChangeFields.MaximumValueLength);
        changes[0].NewValue.Should().HaveLength(SecurityAuditChangeFields.MaximumValueLength);
        changes[0].PreviousValue.Should().Be(changes[0].NewValue);
    }

    [Fact]
    public void Records_the_previous_and_new_global_role_sets()
    {
        UserAuditChanges.GlobalRoles(["PlatformUser"], ["PlatformUser", "PlatformAdmin"])
            .Should().Equal(new SecurityAuditChange(
                SecurityAuditChangeFields.GlobalRole,
                "PlatformUser",
                "PlatformAdmin,PlatformUser"));
        UserAuditChanges.GlobalRoles([], ["PlatformAdmin"])
            .Should().Equal(new SecurityAuditChange(
                SecurityAuditChangeFields.GlobalRole,
                SecurityAuditReasonCatalog.TenantRoleStateNone,
                "PlatformAdmin"));
    }

    [Fact]
    public void Records_creation_as_values_without_a_previous_value_and_keeps_them_within_the_audit_bound()
    {
        string longName = new('a', SecurityAuditChangeFields.MaximumValueLength + 10);

        IReadOnlyList<SecurityAuditChange> changes = UserAuditChanges.Between((null, null, null, null), ("new@example.test", null, longName, null));

        changes.Should().HaveCount(2);
        changes[0].Should().Be(new SecurityAuditChange(SecurityAuditChangeFields.Email, null, "new@example.test"));
        changes[1].NewValue.Should().HaveLength(SecurityAuditChangeFields.MaximumValueLength);
        SecurityAuditChangeFields.AreValid(changes).Should().BeTrue();
    }
}
