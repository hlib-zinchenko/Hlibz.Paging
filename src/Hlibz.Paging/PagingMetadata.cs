using System.Diagnostics.CodeAnalysis;

namespace Hlibz.Paging;

/// <summary>
/// Where a page sits in the whole list.
/// </summary>
/// <remarks>
/// Every property is <see langword="required"/>, so a JSON schema of it (an OpenAPI document,
/// say) lists them all as required.
/// </remarks>
public sealed class PagingMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PagingMetadata"/> class, for serializers and
    /// object initializers. To work out the page counts from a total, use
    /// <see cref="PagingMetadata(int, PagingParams)"/>.
    /// </summary>
    public PagingMetadata()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PagingMetadata"/> class, working out
    /// <see cref="TotalPages"/> and <see cref="HasNextPage"/>.
    /// </summary>
    /// <param name="totalItems">The number of items in the whole list.</param>
    /// <param name="pagingParams">The page.</param>
    [SetsRequiredMembers]
    public PagingMetadata(int totalItems, PagingParams pagingParams)
    {
        ArgumentNullException.ThrowIfNull(pagingParams);
        TotalItems = totalItems;
        CurrentPage = pagingParams.Page;
        PerPage = pagingParams.PerPage;
        TotalPages = (int)Math.Ceiling(totalItems / (double)pagingParams.PerPage);
        HasNextPage = CurrentPage < TotalPages;
    }

    /// <summary>
    /// Gets the number of items in the whole list.
    /// </summary>
    public required int TotalItems { get; init; }

    /// <summary>
    /// Gets the 1-based number of this page.
    /// </summary>
    public required int CurrentPage { get; init; }

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public required int PerPage { get; init; }

    /// <summary>
    /// Gets the number of pages; <c>0</c> for an empty list.
    /// </summary>
    public required int TotalPages { get; init; }

    /// <summary>
    /// Gets a value indicating whether a page follows this one.
    /// </summary>
    public required bool HasNextPage { get; init; }
}
