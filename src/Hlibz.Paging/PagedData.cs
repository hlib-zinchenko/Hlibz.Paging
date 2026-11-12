namespace Hlibz.Paging;

/// <summary>
/// One page of a list, with its <see cref="PagingMetadata"/>.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class PagedData<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PagedData{T}"/> class.
    /// </summary>
    /// <param name="data">The items on the page.</param>
    /// <param name="totalItems">The number of items in the whole list.</param>
    /// <param name="pagingParams">The page.</param>
    public PagedData(IReadOnlyList<T> data, int totalItems, PagingParams pagingParams)
        : this(data, new PagingMetadata(totalItems, pagingParams))
    {
    }

    private PagedData(IReadOnlyList<T> data, PagingMetadata pagingMetadata)
    {
        ArgumentNullException.ThrowIfNull(data);
        Data = data;
        PagingMetadata = pagingMetadata;
    }

    /// <summary>
    /// Gets the items on the page.
    /// </summary>
    public IReadOnlyList<T> Data { get; }

    /// <summary>
    /// Gets where the page sits in the whole list.
    /// </summary>
    public PagingMetadata PagingMetadata { get; }

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
        return new PagedData<TDestination>([.. Data.Select(map)], PagingMetadata);
    }
}
