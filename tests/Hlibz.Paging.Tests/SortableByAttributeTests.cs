using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Hlibz.Paging.Tests;

public sealed class SortableByAttributeTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("customer_name:desc,ID")]
    public void Validate_WithSortableKeys_IsValid(string? sort)
    {
        List<ValidationResult> results = Validate(new OrdersQuery { Sort = sort });

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_WithUnsortableKey_NamesWhatIsSortableInItsCasing()
    {
        ValidationResult result = Assert.Single(Validate(new OrdersQuery { Sort = "total" }));

        Assert.Equal("Can't sort by total. Sort by: id, customer_name, created_at.", result.ErrorMessage);
        Assert.Equal(["Sort"], result.MemberNames);
    }

    [Fact]
    public void Validate_WithoutNamingPolicy_NamesPropertiesAsDeclared()
    {
        ValidationResult result = Assert.Single(Validate(new DeclaredQuery { Sort = "total" }));

        Assert.Equal("Can't sort by total. Sort by: Id, CustomerName, CreatedAt.", result.ErrorMessage);
    }

    [Fact]
    public void Validate_WithErrorMessage_UsesIt()
    {
        ValidationResult result = Assert.Single(Validate(new CustomMessageQuery { Sort = "total" }));

        Assert.Equal("Sort isn't a sort string.", result.ErrorMessage);
    }

    [Fact]
    public void IsValid_WithoutContext_ChecksTheSortString()
    {
        SortableByAttribute attribute = new(typeof(OrderDto));

        Assert.True(attribute.IsValid("created_at"));
        Assert.False(attribute.IsValid("total"));
    }

    [Fact]
    public void IsValid_WithNonString_Throws()
    {
        SortableByAttribute attribute = new(typeof(OrderDto));

        Assert.Throws<InvalidOperationException>(() => attribute.IsValid(42));
    }

    [Fact]
    public void ConvertName_WithNamingPolicy_NamesPropertyAsClientsSeeIt()
    {
        SortableByAttribute attribute = new(typeof(OrderDto)) { NamingPolicy = JsonKnownNamingPolicy.KebabCaseLower };

        Assert.Equal("customer-name", attribute.ConvertName("CustomerName"));
        Assert.Equal("CustomerName", new SortableByAttribute(typeof(OrderDto)).ConvertName("CustomerName"));
    }

    [Fact]
    public void Parse_WithValidSort_ReturnsSorting()
    {
        SortingParams sorting = SortableDto.For<OrderDto>().Parse("customer_name");

        Assert.Equal("CustomerName", Assert.Single(sorting.Params).SortBy);
    }

    [Fact]
    public void Parse_WithInvalidSort_Throws()
    {
        FormatException exception =
            Assert.Throws<FormatException>(() => SortableDto.For<OrderDto>().Parse("total"));

        Assert.Equal("Can't sort by total. Sort by: Id, CustomerName, CreatedAt.", exception.Message);
    }

    private static List<ValidationResult> Validate(object query)
    {
        List<ValidationResult> results = [];
        Validator.TryValidateObject(query, new ValidationContext(query), results, validateAllProperties: true);
        return results;
    }

    private sealed class OrdersQuery
    {
        [SortableBy(typeof(OrderDto), NamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
        public string? Sort { get; init; }
    }

    private sealed class DeclaredQuery
    {
        [SortableBy(typeof(OrderDto))]
        public string? Sort { get; init; }
    }

    private sealed class CustomMessageQuery
    {
        [SortableBy(typeof(OrderDto), ErrorMessage = "{0} isn't a sort string.")]
        public string? Sort { get; init; }
    }
}
