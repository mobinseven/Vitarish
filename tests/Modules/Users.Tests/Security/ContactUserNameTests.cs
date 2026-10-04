using FluentAssertions;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Tests.Security;

public sealed class ContactUserNameTests
{
    [Fact]
    public void Email_changed_moves_username_to_the_new_email()
    {
        User user = new() { UserName = "old@example.test" };

        ContactUserName.MoveWithContact(user, "old@example.test", null, "new@example.test", null);

        user.UserName.Should().Be("new@example.test");
    }

    [Fact]
    public void Phone_changed_moves_username_to_the_new_phone()
    {
        User user = new() { UserName = "+989121234567" };

        ContactUserName.MoveWithContact(user, null, "+989121234567", null, "+9891299999999");

        user.UserName.Should().Be("+9891299999999");
    }

    [Fact]
    public void Email_cleared_moves_username_to_the_phone()
    {
        User user = new() { UserName = "old@example.test" };

        ContactUserName.MoveWithContact(user, "old@example.test", "+989121234567", null, "+989121234567");

        user.UserName.Should().Be("+989121234567");
    }

    [Fact]
    public void Phone_cleared_moves_username_to_the_email()
    {
        User user = new() { UserName = "+989121234567" };

        ContactUserName.MoveWithContact(user, "new@example.test", "+989121234567", "new@example.test", null);

        user.UserName.Should().Be("new@example.test");
    }

    [Fact]
    public void Unrelated_username_is_unchanged()
    {
        User user = new() { UserName = "independently-chosen" };

        ContactUserName.MoveWithContact(user, "old@example.test", "+989121234567", "new@example.test", "+9891299999999");

        user.UserName.Should().Be("independently-chosen");
    }

    [Fact]
    public void Email_changed_only_in_case_moves_username_to_the_new_casing()
    {
        User user = new() { UserName = "Old@Example.Test" };

        ContactUserName.MoveWithContact(user, "Old@Example.Test", null, "old@example.test", null);

        user.UserName.Should().Be("old@example.test");
    }
}
