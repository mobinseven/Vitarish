using System.Text.Json;
using System.Text.Json.Nodes;
using Vitarish.Contents.Domain.Enums;
using Vitarish.Contents.Domain.Models;

namespace Vitarish.Contents.Tests.Domain;

public sealed class ContentFieldsRulesTests
{
    private const string TitleKey = "title";
    private static readonly string[] SiteLocales = ["en", "fa"];

    [Fact]
    public void Draft_accepts_missing_required_field()
    {
        ContentFieldsRules.Validate(
            [RequiredTitle()],
            ParseFields(new JsonObject()),
            SiteLocales,
            ContentFieldsValidationMode.Draft).Should().BeEmpty();
    }

    [Fact]
    public void Publish_rejects_missing_required_field()
    {
        ContentFieldsRules.Validate(
            [RequiredTitle()],
            ParseFields(new JsonObject()),
            SiteLocales,
            ContentFieldsValidationMode.Publish).Should().Contain(error => error.Target == $"{ContentFieldsRules.RootTarget}.{TitleKey}");
    }

    [Fact]
    public void Publish_rejects_missing_site_locale_for_localized_field()
    {
        ContentFieldsRules.Validate(
            [RequiredTitle(localized: true)],
            ParseFields(new JsonObject
            {
                [TitleKey] = new JsonObject { [SiteLocales[0]] = "Purpose" }
            }),
            SiteLocales,
            ContentFieldsValidationMode.Publish).Should().Contain(error => error.Target == $"{ContentFieldsRules.RootTarget}.{TitleKey}.{SiteLocales[1]}");
    }

    [Fact]
    public void Rejects_value_with_wrong_kind()
    {
        ContentFieldsRules.Validate(
            [RequiredTitle()],
            ParseFields(new JsonObject { [TitleKey] = 42 }),
            SiteLocales,
            ContentFieldsValidationMode.Draft).Should().Contain(error => error.Target == $"{ContentFieldsRules.RootTarget}.{TitleKey}");
    }

    private static ContentFieldDefinition RequiredTitle(bool localized = false) => new()
    {
        Key = TitleKey,
        Label = TitleKey,
        Kind = ContentFieldKind.Text,
        Localized = localized,
        Required = true
    };

    private static JsonElement ParseFields(JsonObject fields)
    {
        using JsonDocument document = JsonDocument.Parse(fields.ToJsonString());
        return document.RootElement.Clone();
    }
}
