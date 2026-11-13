using System.Diagnostics.CodeAnalysis;

namespace Hlibz.Paging;

/// <summary>
/// One page of a list, with its <see cref="PagingMetadata"/>.
/// </summary>
/// <remarks>
/// Every property is <see langword="required"/>, so a JSON schema of the page (an OpenAPI
/// document, say) lists them all as required.
/// </remarks>
/// <typeparam name="T">The item type.</typeparam>
public sealed class PagedData<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PagedData{T}"/> class, for serializers and
    /// object initializers. To build a page from a count, use
    /// <see cref="PagedData{T}(IReadOnlyList{T}, int, PagingParams)"/>.
    /// </summary>
    public PagedData()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PagedData{T}"/> class.
    /// </summary>
    /// <param name="data">The items on the page.</param>
    /// <param name="totalItems">The number of items in the whole list.</param>
    /// <param name="pagingParams">The page.</param>
    [SetsRequiredMembers]
    public PagedData(IReadOnlyList<T> data, int totalItems, PagingParams pagingParams)
    {
        ArgumentNullException.ThrowIfNull(data);
        Data = data;
        PagingMetadata = new PagingMetadata(totalItems, pagingParams);
    }

    /// <summary>
    /// Gets the items on the page.
    /// </summary>
    public required IReadOnlyList<T> Data { get; init; }

    /// <summary>
    /// Gets where the page sits in the whole list.
    /// </summary>
    public required PagingMetadata PagingMetadata { get; init; }

    /// <summary>
    /// An empty list's page.
    /// </summary>
    /// <param name="pagingParams">The page asked for.</param>
    /// <returns>The page, with no items.</returns>
    public static PagedData<T> Empty(PagingParams pagingParams) => new([], 0, pagingParams);

    /// <summary>
    /// Converts each item, keeping the paging metadata.
    /// </summary>
    /// <typeparam name="TDestination">The converted item type.</typeparam>
    /// <param name="map">The conversion.</param>
    /// <returns>The same page of converted items.</returns>
    public PagedData<TDestination> Map<TDestination>(Func<T, TDestination> map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return new PagedData<TDestination> { Data = [.. Data.Select(map)], PagingMetadata = PagingMetadata };
    }
}
