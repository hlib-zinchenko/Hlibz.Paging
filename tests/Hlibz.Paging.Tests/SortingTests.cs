namespace Hlibz.Paging.Tests;

public sealed class SortingTests
{
    private static readonly Item[] Items =
    [
        new(3, "banana", 2, new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc)),
        new(1, "Apple", 1, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
        new(5, "cherry", 2, new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)),
        new(2, "apricot", 1, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)),
        new(4, "Blueberry", 3, new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc)),
    ];

    [Fact]
    public void OrderBy_WithString_SortsIgnoringCase()
    {
        int[] ids = Sort(SortingParams.Create().Ascending("Name"));

        Assert.Equal([1, 2, 3, 4, 5], ids);
    }

    [Theory]
    [InlineData("CreatedAt")]
    [InlineData("createdAt")]
    [InlineData("created_at")]
    [InlineData("created-at")]
    public void OrderBy_WithAnyCasing_FindsProperty(string sortBy)
    {
        int[] ids = Sort(SortingParams.Create().Descending(sortBy));

        Assert.Equal([5, 4, 3, 2, 1], ids);
    }

    [Fact]
    public void OrderBy_WithSeveralKeys_BreaksTiesInOrder()
    {
        int[] ids = Sort(SortingParams.Create().Ascending("rank").Descending("name"));

        Assert.Equal([2, 1, 5, 3, 4], ids);
    }

    [Fact]
    public void OrderBy_WithoutKeys_SortsById()
    {
        Assert.Equal([1, 2, 3, 4, 5], Sort(SortingParams.Create()));
    }

    [Fact]
    public void OrderBy_WithEqualKeys_BreaksTiesById()
    {
        Item[] sameName = [Items[0] with { Name = "same" }, Items[1] with { Name = "same" }, Items[3] with { Name = "same" }];

        int[] ids = [.. sameName.AsQueryable().OrderBy(SortingParams.Create().Ascending("name")).Select(item => item.Id)];

        Assert.Equal([1, 2, 3], ids);
    }

    [Fact]
    public void OrderBy_WithIdKey_DoesNotSortByIdTwice()
    {
        IQueryable<Item> sorted = Items.AsQueryable().OrderBy(SortingParams.Create().Descending("id"));

        Assert.Equal([5, 4, 3, 2, 1], sorted.Select(item => item.Id));
        Assert.DoesNotContain("ThenBy", sorted.Expression.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void OrderBy_InMemoryWithNullString_SortsItFirst()
    {
        Person[] people = [new(1, "bob"), new(2, null), new(3, "Alice")];

        int[] ids = [.. people.AsQueryable().OrderBy(SortingParams.Create().Ascending("name")).Select(person => person.Id)];

        Assert.Equal([2, 3, 1], ids);
    }

    [Fact]
    public void OrderBy_WithUnknownProperties_NamesThemAll()
    {
        SortingParamNotFoundException exception = Assert.Throws<SortingParamNotFoundException>(
            () => Sort(SortingParams.Create().Ascending("price").Ascending("name").Descending("color")));

        Assert.Equal(["price", "color"], exception.PropertyNames);
        Assert.Contains("Item", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void OrderBy_WithoutKeysOrId_Throws()
    {
        string[] words = ["b", "a"];

        Assert.Throws<SortingParamNotFoundException>(
            () => words.AsQueryable().OrderBy(SortingParams.Create()).ToArray());
    }

    [Fact]
    public void Paginate_WithPage_SortsThenPages()
    {
        PagedData<Item> page = Items.Paginate(
            SortingParams.Create().Ascending("name"),
            new PagingParams(2, 2));

        Assert.Equal([3, 4], page.Data.Select(item => item.Id));
        Assert.Equal(5, page.PagingMetadata.TotalItems);
        Assert.Equal(3, page.PagingMetadata.TotalPages);
        Assert.True(page.PagingMetadata.HasNextPage);
    }

    [Fact]
    public void Paginate_WithPageAfterTheLast_ReturnsNoItems()
    {
        PagedData<Item> page = Items.Paginate(SortingParams.Create(), new PagingParams(4, 2));

        Assert.Empty(page.Data);
        Assert.Equal(5, page.PagingMetadata.TotalItems);
    }

    [Fact]
    public void Paginate_WithAll_ReturnsEverythingSorted()
    {
        PagedData<Item> page = Items.Paginate(SortingParams.Create(), PagingParams.All);

        Assert.Equal([1, 2, 3, 4, 5], page.Data.Select(item => item.Id));
        Assert.Equal(1, page.PagingMetadata.TotalPages);
    }

    private static int[] Sort(SortingParams sorting) =>
        [.. Items.AsQueryable().OrderBy(sorting).Select(item => item.Id)];

    public sealed record Item(int Id, string Name, int Rank, DateTime CreatedAt);

    public sealed record Person(int Id, string? Name);
}
