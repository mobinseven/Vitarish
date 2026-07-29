using Vitarish.Contents.Contracts.Models;
using Vitarish.Contents.Contracts.Requests;
using Vitarish.Contents.Contracts.Routes;

namespace Vitarish.Contents.Tests.Contracts;

public sealed class ContentsApiContractTests
{
    [Fact]
    public void Routes_keep_item_workflows_under_the_contents_boundary()
    {
        ContentsApiRoutes.Base.Should().Be("/api/contents");
        ContentsApiRoutes.ItemById.Should().Contain("{contentId:guid}");
        ContentsApiRoutes.ArchiveItem.Should().EndWith("/archive");
        ContentsApiRoutes.Revisions.Should().EndWith("/revisions");
    }

    [Fact]
    public void Write_contract_does_not_accept_an_author_identity()
    {
        typeof(ContentItemRequest).GetProperty("AuthorId").Should().BeNull();
        ContentItemRequest request = new(ContentItemType.Article, ContentStatus.Draft, "welcome", "/welcome", "Welcome", "", [], "en");
        request.Status.Should().Be(ContentStatus.Draft);
    }
}
