namespace Hlibz.Paging;

/// <summary>
/// Where a page sits in the whole list.
/// </summary>
public sealed class PagingMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PagingMetadata"/> class.
    /// </summary>
    /// <param name="totalItems">The number of items in the whole list.</param>
    /// <param name="pagingParams">The page.</param>
    public PagingMetadata(int totalItems, PagingParams pagingParams)
    {
        ArgumentNullException.ThrowIfNull(pagingParams);
        TotalItems = totalItems;
        CurrentPage = pagingParams.Page;
        PerPage = pagingParams.PerPage;
    }

    /// <summary>
    /// Gets the number of items in the whole list.
    /// </summary>
    public int TotalItems { get; }

    /// <summary>
    /// Gets the 1-based number of this page.
    /// </summary>
    public int CurrentPage { get; }

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PerPage { get; }

    /// <summary>
    /// Gets the number of pages; <c>0</c> for an empty list.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PerPage);

    /// <summary>
    /// Gets a value indicating whether a page follows this one.
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;
}
