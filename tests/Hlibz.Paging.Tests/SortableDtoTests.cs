using System.ComponentModel;
using System.Text.Json;

namespace Hlibz.Paging.Tests;

public sealed class SortableDtoTests
{
    private static SortableDto Orders => SortableDto.For<OrderDto>();

    [Fact]
    public void For_WithSortableDto_ListsPropertiesAndDefault()
    {
        Type dtoType = typeof(OrderDto);

        Assert.Equal(["Id", "CustomerName", "CreatedAt"], Orders.Properties);
        Assert.Equal("CreatedAt", Orders.DefaultSorting.SortBy);
        Assert.Equal(ListSortDirection.Descending, Orders.DefaultSorting.SortDirection);
        Assert.Same(Orders, SortableDto.For(dtoType));
    }

    [Theory]
    [InlineData(typeof(NoDefaultDto), 0)]
    [InlineData(typeof(TwoDefaultsDto), 2)]
    public void For_WithoutExactlyOneDefault_Throws(Type dtoType, int defaults)
    {
        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() => SortableDto.For(dtoType));

        Assert.Equal(
            $"{dtoType.FullName} must have exactly one property marked with "
            + $"[Sortable(IsDefault = true)], found {defaults}.",
            exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void TryParse_WithoutSort_UsesDefaultOrder(string? sort)
    {
        Assert.True(Orders.TryParse(sort, out SortingParams? sorting, out _));

        Assert.Same(Orders.DefaultSorting, Assert.Single(sorting.Params));
    }

    [Fact]
    public void TryParse_WithKeys_NamesDeclaredProperties()
    {
        Assert.True(Orders.TryParse("customer_name:desc, ID ,createdAt:ASC", out SortingParams? sorting, out _));

        Assert.Equal(["CustomerName", "Id", "CreatedAt"], sorting.Params.Select(key => key.SortBy));
        Assert.Equal(
            [ListSortDirection.Descending, ListSortDirection.Ascending, ListSortDirection.Ascending],
            sorting.Params.Select(key => key.SortDirection));
    }

    [Fact]
    public void TryParse_WithUnsortableProperties_NamesWhatIsSortable()
    {
        Assert.False(Orders.TryParse("note,total:desc,id", out SortingParams? sorting, out string? error));

        Assert.Null(sorting);
        Assert.Equal("Can't sort by note, total. Sort by: Id, CustomerName, CreatedAt.", error);
    }

    [Fact]
    public void TryParse_WithNamingPolicy_NamesPropertiesAsClientsSeeThem()
    {
        Assert.False(Orders.TryParse("total", JsonNamingPolicy.SnakeCaseLower, out _, out string? error));

        Assert.Equal("Can't sort by total. Sort by: id, customer_name, created_at.", error);
    }

    [Theory]
    [InlineData("name:up")]
    [InlineData("name:asc:desc")]
    [InlineData("na me")]
    public void TryParse_WithMalformedKey_Fails(string sort)
    {
        Assert.False(Orders.TryParse(sort, out _, out string? error));

        Assert.StartsWith("Invalid sort key", error, StringComparison.Ordinal);
    }

    [Fact]
    public void TryParse_WithOnlySeparators_Fails()
    {
        Assert.False(Orders.TryParse(" , ,", out _, out string? error));

        Assert.Equal("The sort string names no sort key.", error);
    }

    [Fact]
    public void ValidateAll_WithInvalidDtos_NamesEach()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => SortableDto.ValidateAll([typeof(OrderDto).Assembly]));

        Assert.Equal(
            [
                $"{typeof(NoDefaultDto).FullName} must have exactly one property marked with "
                + "[Sortable(IsDefault = true)], found 0.",
                $"{typeof(TwoDefaultsDto).FullName} must have exactly one property marked with "
                + "[Sortable(IsDefault = true)], found 2.",
            ],
            exception.Message.Split(Environment.NewLine).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void ValidateAll_WithValidDtos_Passes()
    {
        SortableDto.ValidateAll([typeof(SortableDto).Assembly]);
    }

    [Fact]
    public void ValidateAll_WithAbstractBase_SkipsIt()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => SortableDto.ValidateAll([typeof(AuditedDto).Assembly]));

        Assert.DoesNotContain(typeof(AuditedDto).FullName!, exception.Message, StringComparison.Ordinal);
        Assert.Equal(["Id", "UpdatedAt"], SortableDto.For<AuditedOrderDto>().Properties.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void ValidateAll_WithTypes_ChecksOnlyThose()
    {
        SortableDto.ValidateAll([typeof(OrderDto), typeof(AuditedOrderDto)]);
    }

    [Fact]
    public void ValidateAll_WithInvalidTypes_NamesEach()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => SortableDto.ValidateAll([typeof(OrderDto), typeof(NoDefaultDto)]));

        Assert.Equal(
            $"{typeof(NoDefaultDto).FullName} must have exactly one property marked with "
            + "[Sortable(IsDefault = true)], found 0.",
            exception.Message);
    }
}
