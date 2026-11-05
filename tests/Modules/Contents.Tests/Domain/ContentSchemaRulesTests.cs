using Vitarish.Contents.Domain.Enums;
using Vitarish.Contents.Domain.Models;

namespace Vitarish.Contents.Tests.Domain;

public sealed class ContentSchemaRulesTests
{
    private const string TitleKey = "title";
    private const string OptionalKey = "summary";

    [Fact]
    public void Validate_accepts_valid_schema()
    {
        ContentSchemaRules.Validate([Field(TitleKey, ContentFieldKind.Text, required: true)]).Should().BeEmpty();
    }

    [Fact]
    public void Validate_rejects_duplicate_keys()
    {
        ContentSchemaRules.Validate([
            Field(TitleKey, ContentFieldKind.Text),
            Field(TitleKey, ContentFieldKind.LongText)
        ]).Should().NotBeEmpty();
    }

    [Fact]
    public void EnsureAdditive_rejects_kind_change()
    {
        ContentSchemaRules.EnsureAdditive(
            [Field(TitleKey, ContentFieldKind.Text)],
            [Field(TitleKey, ContentFieldKind.LongText)]).Should().NotBeEmpty();
    }

    [Fact]
    public void EnsureAdditive_rejects_localization_change()
    {
        ContentSchemaRules.EnsureAdditive(
            [Field(TitleKey, ContentFieldKind.Text)],
            [Field(TitleKey, ContentFieldKind.Text, localized: true)]).Should().NotBeEmpty();
    }

    [Fact]
    public void EnsureAdditive_rejects_removed_field()
    {
        ContentSchemaRules.EnsureAdditive(
            [Field(TitleKey, ContentFieldKind.Text)],
            []).Should().NotBeEmpty();
    }

    [Fact]
    public void EnsureAdditive_rejects_new_required_field()
    {
        ContentSchemaRules.EnsureAdditive(
            [Field(TitleKey, ContentFieldKind.Text)],
            [Field(TitleKey, ContentFieldKind.Text), Field(OptionalKey, ContentFieldKind.Text, required: true)])
            .Should().NotBeEmpty();
    }

    [Fact]
    public void EnsureAdditive_accepts_new_optional_field()
    {
        ContentSchemaRules.EnsureAdditive(
            [Field(TitleKey, ContentFieldKind.Text)],
            [Field(TitleKey, ContentFieldKind.Text), Field(OptionalKey, ContentFieldKind.Text)])
            .Should().BeEmpty();
    }

    private static ContentFieldDefinition Field(
        string key,
        ContentFieldKind kind,
        bool localized = false,
        bool required = false) => new()
    {
        Key = key,
        Label = key,
        Kind = kind,
        Localized = localized,
        Required = required
    };
}
