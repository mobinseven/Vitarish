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
    public void Treats_a_case_only_email_difference_and_blank_values_as_unchanged()
    {
        UserAuditChanges.Between(("User@Example.test", null, " ", null), ("user@example.test", "", null, null))
            .Should().BeEmpty();
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
