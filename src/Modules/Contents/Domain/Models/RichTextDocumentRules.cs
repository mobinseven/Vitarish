using System.Text;
using System.Text.Json;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Contents.Domain.Models;

public static class RichTextDocumentRules
{
    public const int MaximumSerializedBytes = 200 * 1024;
    public const int MaximumDepth = 12;
    public const int MaximumNodes = 2000;

    public const string TypeProperty = "type";
    public const string ContentProperty = "content";
    public const string AttributesProperty = "attrs";
    public const string TextProperty = "text";
    public const string MarksProperty = "marks";
    public const string LevelProperty = "level";
    public const string HrefProperty = "href";
    public const string TargetProperty = "target";

    public const string DocumentNode = "doc";
    public const string ParagraphNode = "paragraph";
    public const string HeadingNode = "heading";
    public const string BulletListNode = "bulletList";
    public const string OrderedListNode = "orderedList";
    public const string ListItemNode = "listItem";
    public const string BlockquoteNode = "blockquote";
    public const string HardBreakNode = "hardBreak";
    public const string TextNode = "text";

    public const string BoldMark = "bold";
    public const string ItalicMark = "italic";
    public const string LinkMark = "link";
    public const string BlankTarget = "_blank";

    public static IReadOnlyList<EntityError> Validate(JsonElement document, string target = ContentFieldsRules.RootTarget)
    {
        List<EntityError> errors = [];
        if (document.ValueKind != JsonValueKind.Object)
        {
            errors.Add(EntityError.Validation("Rich text must be a Tiptap document object.", target));
            return errors;
        }

        if (Encoding.UTF8.GetByteCount(document.GetRawText()) > MaximumSerializedBytes)
        {
            errors.Add(EntityError.Validation($"Rich text cannot exceed {MaximumSerializedBytes} serialized bytes.", target));
            return errors;
        }

        int nodeCount = 0;
        ValidateNode(document, target, 1, null, errors, ref nodeCount);
        if (nodeCount > MaximumNodes)
        {
            errors.Add(EntityError.Validation($"Rich text cannot contain more than {MaximumNodes} nodes.", target));
        }

        return errors;
    }

    private static void ValidateNode(
        JsonElement node,
        string path,
        int depth,
        string? parentType,
        List<EntityError> errors,
        ref int nodeCount)
    {
        nodeCount++;
        if (depth > MaximumDepth)
        {
            errors.Add(EntityError.Validation($"Rich text nesting cannot exceed {MaximumDepth} levels.", path));
            return;
        }

        if (node.ValueKind != JsonValueKind.Object || !TryGetString(node, TypeProperty, out string type))
        {
            errors.Add(EntityError.Validation("Each rich text node must be an object with a node type.", path));
            return;
        }

        if (!IsAllowedNode(type))
        {
            errors.Add(EntityError.Validation("Rich text contains a node type that is not allowed.", $"{path}.{TypeProperty}"));
            return;
        }

        if (!IsAllowedChild(parentType, type))
        {
            errors.Add(EntityError.Validation("Rich text node is not allowed in this position.", path));
        }

        ValidateNodeProperties(node, type, path, errors);
        if (type == TextNode)
        {
            if (!TryGetString(node, TextProperty, out _))
            {
                errors.Add(EntityError.Validation("Text nodes require string text.", $"{path}.{TextProperty}"));
            }

            ValidateMarks(node, path, errors);
        }

        if (type == HeadingNode)
        {
            ValidateHeadingAttributes(node, path, errors);
        }

        bool requiresChildren = type is DocumentNode or BulletListNode or OrderedListNode or ListItemNode or BlockquoteNode;
        if (node.TryGetProperty(ContentProperty, out JsonElement content))
        {
            if (content.ValueKind != JsonValueKind.Array)
            {
                errors.Add(EntityError.Validation("Rich text child content must be an array.", $"{path}.{ContentProperty}"));
                return;
            }

            if (requiresChildren && content.GetArrayLength() == 0)
            {
                errors.Add(EntityError.Validation("This rich text node requires non-empty child content.", $"{path}.{ContentProperty}"));
            }

            int index = 0;
            foreach (JsonElement child in content.EnumerateArray())
            {
                ValidateNode(child, $"{path}.{ContentProperty}[{index}]", depth + 1, type, errors, ref nodeCount);
                index++;
            }
        }
        else if (requiresChildren)
        {
            errors.Add(EntityError.Validation("This rich text node requires child content.", $"{path}.{ContentProperty}"));
        }
    }

    private static void ValidateNodeProperties(JsonElement node, string type, string path, List<EntityError> errors)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (JsonProperty property in node.EnumerateObject())
        {
            if (!seen.Add(property.Name))
            {
                errors.Add(EntityError.Validation("Rich text node properties cannot be duplicated.", $"{path}.{property.Name}"));
            }

            bool allowed = property.Name switch
            {
                TypeProperty => true,
                ContentProperty => type is DocumentNode or ParagraphNode or HeadingNode or BulletListNode or OrderedListNode or ListItemNode or BlockquoteNode,
                AttributesProperty => type == HeadingNode,
                TextProperty => type == TextNode,
                MarksProperty => type == TextNode,
                _ => false
            };

            if (!allowed)
            {
                errors.Add(EntityError.Validation("Rich text node contains an unsupported property.", $"{path}.{property.Name}"));
            }
        }
    }

    private static void ValidateHeadingAttributes(JsonElement node, string path, List<EntityError> errors)
    {
        if (!node.TryGetProperty(AttributesProperty, out JsonElement attributes) || attributes.ValueKind != JsonValueKind.Object ||
            !attributes.TryGetProperty(LevelProperty, out JsonElement level) || level.ValueKind != JsonValueKind.Number ||
            !level.TryGetInt32(out int headingLevel) || headingLevel is < 2 or > 4)
        {
            errors.Add(EntityError.Validation("Heading level must be 2, 3, or 4.", $"{path}.{AttributesProperty}.{LevelProperty}"));
            return;
        }

        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (JsonProperty property in attributes.EnumerateObject())
        {
            if (!seen.Add(property.Name))
            {
                errors.Add(EntityError.Validation("Heading attributes cannot be duplicated.", $"{path}.{AttributesProperty}.{property.Name}"));
            }
            else if (property.Name != LevelProperty)
            {
                errors.Add(EntityError.Validation("Heading attributes contain an unsupported property.", $"{path}.{AttributesProperty}.{property.Name}"));
            }
        }
    }

    private static void ValidateMarks(JsonElement node, string path, List<EntityError> errors)
    {
        if (!node.TryGetProperty(MarksProperty, out JsonElement marks))
        {
            return;
        }

        if (marks.ValueKind != JsonValueKind.Array)
        {
            errors.Add(EntityError.Validation("Text marks must be an array.", $"{path}.{MarksProperty}"));
            return;
        }

        int index = 0;
        foreach (JsonElement mark in marks.EnumerateArray())
        {
            string markPath = $"{path}.{MarksProperty}[{index}]";
            index++;

            if (mark.ValueKind != JsonValueKind.Object || !TryGetString(mark, TypeProperty, out string type) ||
                type is not (BoldMark or ItalicMark or LinkMark))
            {
                errors.Add(EntityError.Validation("Text contains an unsupported mark.", markPath));
                continue;
            }

            HashSet<string> seen = new(StringComparer.Ordinal);
            foreach (JsonProperty property in mark.EnumerateObject())
            {
                if (!seen.Add(property.Name))
                {
                    errors.Add(EntityError.Validation("Rich text mark properties cannot be duplicated.", $"{markPath}.{property.Name}"));
                }
                else if (property.Name != TypeProperty && property.Name != AttributesProperty)
                {
                    errors.Add(EntityError.Validation("Rich text mark contains an unsupported property.", $"{markPath}.{property.Name}"));
                }
            }

            if (type == LinkMark)
            {
                ValidateLinkAttributes(mark, markPath, errors);
            }
            else if (mark.TryGetProperty(AttributesProperty, out _))
            {
                errors.Add(EntityError.Validation("Bold and italic marks do not have attributes.", $"{markPath}.{AttributesProperty}"));
            }
        }
    }

    private static void ValidateLinkAttributes(JsonElement mark, string path, List<EntityError> errors)
    {
        string attributesPath = $"{path}.{AttributesProperty}";
        if (!mark.TryGetProperty(AttributesProperty, out JsonElement attributes) || attributes.ValueKind != JsonValueKind.Object ||
            !TryGetString(attributes, HrefProperty, out string href))
        {
            errors.Add(EntityError.Validation("Link marks require a valid href attribute.", $"{attributesPath}.{HrefProperty}"));
            return;
        }

        if (!ContentHrefRules.IsAllowed(href))
        {
            errors.Add(EntityError.Validation("Link href must use HTTPS or a single-leading-slash site-relative path.", $"{attributesPath}.{HrefProperty}"));
        }

        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (JsonProperty property in attributes.EnumerateObject())
        {
            if (!seen.Add(property.Name))
            {
                errors.Add(EntityError.Validation("Link attributes cannot be duplicated.", $"{attributesPath}.{property.Name}"));
            }
            else if (property.Name == TargetProperty)
            {
                if (property.Value.ValueKind != JsonValueKind.String || property.Value.GetString() != BlankTarget)
                {
                    errors.Add(EntityError.Validation("Link target may only be _blank.", $"{attributesPath}.{TargetProperty}"));
                }
            }
            else if (property.Name != HrefProperty)
            {
                errors.Add(EntityError.Validation("Link attributes contain an unsupported property.", $"{attributesPath}.{property.Name}"));
            }
        }
    }

    private static bool IsAllowedNode(string type) => type is
        DocumentNode or ParagraphNode or HeadingNode or BulletListNode or OrderedListNode or ListItemNode or
        BlockquoteNode or HardBreakNode or TextNode;

    private static bool IsAllowedChild(string? parentType, string childType) => parentType switch
    {
        null => childType == DocumentNode,
        DocumentNode or ListItemNode or BlockquoteNode => childType is ParagraphNode or HeadingNode or BulletListNode or OrderedListNode or BlockquoteNode,
        ParagraphNode or HeadingNode => childType is TextNode or HardBreakNode,
        BulletListNode or OrderedListNode => childType == ListItemNode,
        _ => false
    };

    private static bool TryGetString(JsonElement element, string propertyName, out string value)
    {
        if (element.TryGetProperty(propertyName, out JsonElement property) && property.ValueKind == JsonValueKind.String)
        {
            string? stringValue = property.GetString();
            if (stringValue is not null)
            {
                value = stringValue;
                return true;
            }
        }

        value = string.Empty;
        return false;
    }
}
