using System.Text.Json.Nodes;
using Vitarish.Contents.Application.Abstractions;
using Vitarish.Contents.Domain.Enums;
using Vitarish.Contents.Domain.Models;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Landing.Application;

public static class LandingContentSeed
{
    private const string PersianLocale = "fa";
    private const string EnglishLocale = "en";
    private const string PurposeTitleEnglish = "What matters to you is where we begin.";
    private const string PurposeTitlePersian = "از آنچه برای شما مهم است آغاز می‌کنیم.";
    private const string PurposeCategoryEnglish = "Our purpose";
    private const string PurposeCategoryPersian = "هدف ما";
    private const string PurposeDescriptionEnglish = "Your work has a purpose beyond the tasks that fill your day. We build with that purpose in mind: less friction between what you need to do and what you want to achieve.";
    private const string PurposeDescriptionPersian = "کار شما هدفی فراتر از وظایف روزمره دارد. ما با توجه به همان هدف می‌سازیم؛ برای کم کردن فاصلهٔ میان کارهایی که باید انجام دهید و آنچه می‌خواهید به دست آورید.";
    private const string ApplicationsHeadingEnglish = "Our purpose. In practice.";
    private const string ApplicationsHeadingPersian = "هدف ما، در عمل.";
    private const string ApplicationsIntroductionEnglish = "Darman brings the daily work of a private practice together. One place for your schedule, your patients and your team.";
    private const string ApplicationsIntroductionPersian = "درمان کارهای روزمرهٔ مطب را کنار هم می‌آورد؛ جایی برای برنامهٔ کاری، بیماران و همکاران شما.";
    private const string PracticeDescriptionEnglish = "Manage appointments, shifts and services. Keep patient records and your team connected to the work of your practice.";
    private const string PracticeDescriptionPersian = "نوبت‌ها، شیفت‌ها و خدمات را مدیریت کنید. پروندهٔ بیماران و کار همکاران را در کنار هم داشته باشید.";
    private const string PracticeNoteEnglish = "Free for your practice. No time limit.";
    private const string PracticeNotePersian = "برای مطب شما رایگان است؛ بدون محدودیت زمانی.";
    private const string DiscoverDarmanEnglish = "Discover Darman";
    private const string DiscoverDarmanPersian = "آشنایی با درمان";

    public const string BackgroundResource = "landing:content-seed";

    public static async Task EnsureAsync(
        IContentSeeding contentSeeding,
        string tenantId,
        CancellationToken cancellationToken)
    {
        string[] locales = [PersianLocale, EnglishLocale];
        EntityResult siteResult = await contentSeeding.EnsureSiteAsync(
            tenantId,
            LandingContent.SiteKey,
            LandingTenant.Name,
            PersianLocale,
            locales,
            cancellationToken);
        EnsureSucceeded(siteResult, nameof(IContentSeeding.EnsureSiteAsync));

        ContentFieldDefinition[] fields =
        [
            new() { Key = LandingContent.TitleFieldKey, Label = "Title", Kind = ContentFieldKind.Text, Required = true, Localized = true },
            new() { Key = LandingContent.CategoryFieldKey, Label = "Category", Kind = ContentFieldKind.Text, Localized = true },
            new() { Key = LandingContent.BodyFieldKey, Label = "Body", Kind = ContentFieldKind.RichText, Required = true, Localized = true },
            new() { Key = LandingContent.SeoDescriptionFieldKey, Label = "SEO description", Kind = ContentFieldKind.LongText, Localized = true }
        ];
        EntityResult typeResult = await contentSeeding.EnsureContentTypeAsync(
            tenantId,
            LandingContent.EditorialPageTypeKey,
            LandingContent.EditorialPageTypeName,
            fields,
            cancellationToken);
        EnsureSucceeded(typeResult, nameof(IContentSeeding.EnsureContentTypeAsync));

        EntityResult entryResult = await contentSeeding.EnsurePublishedEntryAsync(
            tenantId,
            LandingContent.SiteKey,
            LandingContent.EditorialPageTypeKey,
            LandingContent.PurposeSlug,
            LandingContent.PurposePath,
            PurposeTitlePersian,
            PurposeDescriptionPersian,
            BuildFieldsJson(),
            cancellationToken);
        EnsureSucceeded(entryResult, nameof(IContentSeeding.EnsurePublishedEntryAsync));
    }

    private static string BuildFieldsJson() => new JsonObject
    {
        [LandingContent.TitleFieldKey] = new JsonObject
        {
            [EnglishLocale] = PurposeTitleEnglish,
            [PersianLocale] = PurposeTitlePersian
        },
        [LandingContent.CategoryFieldKey] = new JsonObject
        {
            [EnglishLocale] = PurposeCategoryEnglish,
            [PersianLocale] = PurposeCategoryPersian
        },
        [LandingContent.BodyFieldKey] = new JsonObject
        {
            [EnglishLocale] = BuildBody(
                PurposeDescriptionEnglish,
                ApplicationsHeadingEnglish,
                ApplicationsIntroductionEnglish,
                PracticeDescriptionEnglish,
                PracticeNoteEnglish,
                DiscoverDarmanEnglish),
            [PersianLocale] = BuildBody(
                PurposeDescriptionPersian,
                ApplicationsHeadingPersian,
                ApplicationsIntroductionPersian,
                PracticeDescriptionPersian,
                PracticeNotePersian,
                DiscoverDarmanPersian)
        },
        [LandingContent.SeoDescriptionFieldKey] = new JsonObject
        {
            [EnglishLocale] = PurposeDescriptionEnglish,
            [PersianLocale] = PurposeDescriptionPersian
        }
    }.ToJsonString();

    private static JsonObject BuildBody(
        string description,
        string heading,
        string introduction,
        string practiceDescription,
        string practiceNote,
        string discoverText) => new()
    {
        [RichTextDocumentRules.TypeProperty] = RichTextDocumentRules.DocumentNode,
        [RichTextDocumentRules.ContentProperty] = new JsonArray(
            Paragraph(description),
            new JsonObject
            {
                [RichTextDocumentRules.TypeProperty] = RichTextDocumentRules.HeadingNode,
                [RichTextDocumentRules.AttributesProperty] = new JsonObject
                {
                    [RichTextDocumentRules.LevelProperty] = 2
                },
                [RichTextDocumentRules.ContentProperty] = new JsonArray(Text(heading))
            },
            Paragraph(introduction),
            Paragraph(practiceDescription),
            Paragraph(practiceNote),
            new JsonObject
            {
                [RichTextDocumentRules.TypeProperty] = RichTextDocumentRules.ParagraphNode,
                [RichTextDocumentRules.ContentProperty] = new JsonArray(
                    new JsonObject
                    {
                        [RichTextDocumentRules.TypeProperty] = RichTextDocumentRules.TextNode,
                        [RichTextDocumentRules.TextProperty] = discoverText,
                        [RichTextDocumentRules.MarksProperty] = new JsonArray(
                            new JsonObject
                            {
                                [RichTextDocumentRules.TypeProperty] = RichTextDocumentRules.LinkMark,
                                [RichTextDocumentRules.AttributesProperty] = new JsonObject
                                {
                                    [RichTextDocumentRules.HrefProperty] = LandingContent.DarmanUrl
                                }
                            })
                    })
            })
    };

    private static JsonObject Paragraph(string text) => new()
    {
        [RichTextDocumentRules.TypeProperty] = RichTextDocumentRules.ParagraphNode,
        [RichTextDocumentRules.ContentProperty] = new JsonArray(Text(text))
    };

    private static JsonObject Text(string text) => new()
    {
        [RichTextDocumentRules.TypeProperty] = RichTextDocumentRules.TextNode,
        [RichTextDocumentRules.TextProperty] = text
    };

    private static void EnsureSucceeded(EntityResult result, string operation)
    {
        if (result.IsFailure)
        {
            string errors = string.Join("; ", result.Errors.Select(error => error.Message));
            throw new InvalidOperationException($"Landing content seeding failed during {operation}: {errors}");
        }
    }
}
