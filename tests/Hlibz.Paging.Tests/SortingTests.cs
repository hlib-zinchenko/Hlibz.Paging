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

    [Fact]
    public void Paginate_WithNullAndMixedCaseStrings_SortsLikeTheQuerySort()
    {
        Person[] people = [new(1, "bob"), new(2, null), new(3, "Alice"), new(4, "alice"), new(5, "Bob")];
        SortingParams sorting = SortingParams.Create().Ascending("name");

        PagedData<Person> page = people.Paginate(sorting, PagingParams.All);

        Assert.Equal([2, 3, 4, 1, 5], page.Data.Select(person => person.Id));
        Assert.Equal(
            people.AsQueryable().OrderBy(sorting).Select(person => person.Id),
            page.Data.Select(person => person.Id));
    }

    [Fact]
    public void Paginate_WithDescendingKeys_SortsNullLastAndBreaksTiesById()
    {
        Person[] people = [new(3, "same"), new(1, null), new(2, "same"), new(4, "other")];

        PagedData<Person> page = people.Paginate(SortingParams.Create().Descending("name"), PagingParams.All);

        Assert.Equal([2, 3, 4, 1], page.Data.Select(person => person.Id));
    }

    [Fact]
    public void Paginate_WithSeveralKeys_MatchesTheQuerySort()
    {
        SortingParams sorting = SortingParams.Create().Ascending("rank").Descending("created_at");

        PagedData<Item> page = Items.Paginate(sorting, PagingParams.All);

        Assert.Equal(Sort(sorting), page.Data.Select(item => item.Id));
    }

    [Fact]
    public void Paginate_WithEqualKeysAndGenericallyComparableId_BreaksTiesById()
    {
        Order[] orders = [new(new OrderId(3), "same"), new(new OrderId(1), "same"), new(new OrderId(2), "other")];

        PagedData<Order> page = orders.Paginate(SortingParams.Create().Ascending("name"), PagingParams.All);

        Assert.Equal([2, 1, 3], page.Data.Select(order => order.Id.Value));
    }

    [Fact]
    public void Paginate_WithGenericallyComparableNullableKey_MatchesTheQuerySort()
    {
        Order[] orders =
        [
            new(new OrderId(1), "a", new OrderId(5)),
            new(new OrderId(2), "b"),
            new(new OrderId(3), "c", new OrderId(4)),
        ];
        SortingParams sorting = SortingParams.Create().Descending("parent_id");

        PagedData<Order> page = orders.Paginate(sorting, PagingParams.All);

        Assert.Equal([1, 3, 2], page.Data.Select(order => order.Id.Value));
        Assert.Equal(
            orders.AsQueryable().OrderBy(sorting).Select(order => order.Id.Value),
            page.Data.Select(order => order.Id.Value));
    }

    [Fact]
    public void Paginate_WithUnknownProperty_Throws()
    {
        Assert.Throws<SortingParamNotFoundException>(
            () => Items.Paginate(SortingParams.Create().Ascending("price"), PagingParams.All));
    }

    private static int[] Sort(SortingParams sorting) =>
        [.. Items.AsQueryable().OrderBy(sorting).Select(item => item.Id)];

    /// <summary>
    /// Comparable only through <see cref="IComparable{T}"/>, as generated strongly-typed ids are.
    /// </summary>
    public readonly record struct OrderId(int Value) : IComparable<OrderId>
    {
        public static bool operator <(OrderId left, OrderId right) => left.CompareTo(right) < 0;

        public static bool operator <=(OrderId left, OrderId right) => left.CompareTo(right) <= 0;

        public static bool operator >(OrderId left, OrderId right) => left.CompareTo(right) > 0;

        public static bool operator >=(OrderId left, OrderId right) => left.CompareTo(right) >= 0;

        public int CompareTo(OrderId other) => Value.CompareTo(other.Value);
    }

    public sealed record Item(int Id, string Name, int Rank, DateTime CreatedAt);

    public sealed record Person(int Id, string? Name);

    public sealed record Order(OrderId Id, string Name, OrderId? ParentId = null);
}
