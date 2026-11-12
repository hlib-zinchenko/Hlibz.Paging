namespace Hlibz.Paging.Tests;

public sealed class PagingTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData(0, 0)]
    [InlineData(-3, -1)]
    public void PagingParams_WithMissingOrBelowOne_UsesDefaults(int? page, int? perPage)
    {
        PagingParams paging = new(page, perPage);

        Assert.Equal(PagingParams.DefaultPage, paging.Page);
        Assert.Equal(PagingParams.DefaultPerPage, paging.PerPage);
    }

    [Fact]
    public void PagingParams_WithPage_SkipsEarlierPages()
    {
        Assert.Equal(20, new PagingParams(3, 10).Skip);
    }

    [Fact]
    public void PagingParams_WithHugePage_CapsSkip()
    {
        Assert.Equal(int.MaxValue, new PagingParams(int.MaxValue, int.MaxValue).Skip);
    }

    [Fact]
    public void PagingParams_All_IsOnePageOfEverything()
    {
        Assert.True(PagingParams.All.IsAll);
        Assert.False(new PagingParams(1, 100).IsAll);
    }

    [Theory]
    [InlineData(25, 2, 10, 3, true)]
    [InlineData(25, 3, 10, 3, false)]
    [InlineData(20, 2, 10, 2, false)]
    [InlineData(0, 1, 10, 0, false)]
    public void PagingMetadata_WithTotal_CountsPages(
        int total,
        int page,
        int perPage,
        int expectedPages,
        bool expectedHasNext)
    {
        PagingMetadata metadata = new(total, new PagingParams(page, perPage));

        Assert.Equal(expectedPages, metadata.TotalPages);
        Assert.Equal(expectedHasNext, metadata.HasNextPage);
    }

    [Fact]
    public void PagedData_Map_KeepsPagingMetadata()
    {
        PagedData<int> page = new([1, 2], 7, new PagingParams(2, 2));

        PagedData<string> mapped = page.Map(item => $"#{item}");

        Assert.Equal(["#1", "#2"], mapped.Data);
        Assert.Same(page.PagingMetadata, mapped.PagingMetadata);
    }

    [Fact]
    public void PagedData_Empty_HasNoItemsOrPages()
    {
        PagedData<int> page = PagedData<int>.Empty(new PagingParams(3, 10));

        Assert.Empty(page.Data);
        Assert.Equal(0, page.PagingMetadata.TotalItems);
        Assert.Equal(3, page.PagingMetadata.CurrentPage);
    }
}
