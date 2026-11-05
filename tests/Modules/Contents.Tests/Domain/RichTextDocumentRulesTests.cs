using System.Text.Json;
using System.Text.Json.Nodes;
using Vitarish.Contents.Domain.Models;

namespace Vitarish.Contents.Tests.Domain;

public sealed class RichTextDocumentRulesTests
{
    [Fact]
    public void Accepts_allowed_nodes_and_marks()
    {
        JsonObject document = new()
        {
            [RichTextDocumentRules.TypeProperty] = RichTextDocumentRules.DocumentNode,
            [RichTextDocumentRules.ContentProperty] = new JsonArray(
                Paragraph(
                    Text("formatted", new JsonObject { [RichTextDocumentRules.TypeProperty] = RichTextDocumentRules.BoldMark }),
                    Text("italic", new JsonObject { [RichTextDocumentRules.TypeProperty] = RichTextDocumentRules.ItalicMark }),
                    Text("linked", new JsonObject
                    {
                        [RichTextDocumentRules.TypeProperty] = RichTextDocumentRules.LinkMark,
                        [RichTextDocumentRules.AttributesProperty] = new JsonObject
                        {
                            [RichTextDocumentRules.HrefProperty] = "/relative-path",
                            [RichTextDocumentRules.TargetProperty] = RichTextDocumentRules.BlankTarget
                        }
                    }),
                    new JsonObject { [RichTextDocumentRules.TypeProperty] = RichTextDocumentRules.HardBreakNode }),
                Heading(),
                List(RichTextDocumentRules.BulletListNode),
                List(RichTextDocumentRules.OrderedListNode),
                new JsonObject
                {
                    [RichTextDocumentRules.TypeProperty] = RichTextDocumentRules.BlockquoteNode,
                    [RichTextDocumentRules.ContentProperty] = new JsonArray(Paragraph(Text("quoted")))
                })
        };

        Validate(document).Should().BeEmpty();
    }

    [Fact]
    public void Rejects_script_like_node()
    {
        JsonObject document = new()
        {
            [RichTextDocumentRules.TypeProperty] = "script"
        };

        Validate(document).Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("//x")]
    public void Rejects_unsafe_link_hrefs(string href)
    {
        JsonObject document = new()
        {
            [RichTextDocumentRules.TypeProperty] = RichTextDocumentRules.DocumentNode,
            [RichTextDocumentRules.ContentProperty] = new JsonArray(
                Paragraph(Text("unsafe", new JsonObject
                {
                    [RichTextDocumentRules.TypeProperty] = RichTextDocumentRules.LinkMark,
                    [RichTextDocumentRules.AttributesProperty] = new JsonObject
                    {
                        [RichTextDocumentRules.HrefProperty] = href
                    }
                })))
        };

        Validate(document).Should().NotBeEmpty();
    }

    [Fact]
    public void Rejects_nesting_beyond_maximum_depth()
    {
        JsonNode nested = Paragraph(Text("deep"));
        for (int index = 0; index < RichTextDocumentRules.MaximumDepth; index++)
        {
            nested = new JsonObject
            {
                [RichTextDocumentRules.TypeProperty] = RichTextDocumentRules.BlockquoteNode,
                [RichTextDocumentRules.ContentProperty] = new JsonArray(nested)
            };
        }

        JsonObject document = new()
        {
            [RichTextDocumentRules.TypeProperty] = RichTextDocumentRules.DocumentNode,
            [RichTextDocumentRules.ContentProperty] = new JsonArray(nested)
        };

        Validate(document).Should().NotBeEmpty();
    }

    [Fact]
    public void Rejects_serialized_size_beyond_maximum()
    {
        JsonObject document = new()
        {
            [RichTextDocumentRules.TypeProperty] = RichTextDocumentRules.DocumentNode,
            [RichTextDocumentRules.ContentProperty] = new JsonArray(
                Paragraph(Text(new string('x', RichTextDocumentRules.MaximumSerializedBytes))))
        };

        Validate(document).Should().NotBeEmpty();
    }

    private static IReadOnlyList<Vitarish.Entities.Contracts.Models.EntityError> Validate(JsonObject document)
    {
        using JsonDocument parsed = JsonDocument.Parse(document.ToJsonString());
        return RichTextDocumentRules.Validate(parsed.RootElement);
    }

    private static JsonObject Paragraph(params JsonObject[] content) => new()
    {
        [RichTextDocumentRules.TypeProperty] = RichTextDocumentRules.ParagraphNode,
        [RichTextDocumentRules.ContentProperty] = new JsonArray(content)
    };

    private static JsonObject Heading() => new()
    {
        [RichTextDocumentRules.TypeProperty] = RichTextDocumentRules.HeadingNode,
        [RichTextDocumentRules.AttributesProperty] = new JsonObject { [RichTextDocumentRules.LevelProperty] = 2 },
        [RichTextDocumentRules.ContentProperty] = new JsonArray(Text("heading"))
    };

    private static JsonObject List(string type) => new()
    {
        [RichTextDocumentRules.TypeProperty] = type,
        [RichTextDocumentRules.ContentProperty] = new JsonArray(new JsonObject
        {
            [RichTextDocumentRules.TypeProperty] = RichTextDocumentRules.ListItemNode,
            [RichTextDocumentRules.ContentProperty] = new JsonArray(Paragraph(Text("item")))
        })
    };

    private static JsonObject Text(string value, params JsonObject[] marks)
    {
        JsonObject text = new()
        {
            [RichTextDocumentRules.TypeProperty] = RichTextDocumentRules.TextNode,
            [RichTextDocumentRules.TextProperty] = value
        };
        if (marks.Length > 0)
        {
            text[RichTextDocumentRules.MarksProperty] = new JsonArray(marks);
        }

        return text;
    }
}
