using System.Diagnostics.CodeAnalysis;

using Microsoft.EntityFrameworkCore;

namespace Hlibz.Paging.EntityFrameworkCore;

/// <summary>
/// Pages Entity Framework Core queries.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Why paging a query isn't trimming- or AOT-safe, as a warning on the caller: it sorts the
    /// query with <c>SortingExtensions.OrderBy</c>.
    /// </summary>
    private const string QuerySortWarning =
        "Sorting a query builds Queryable.OrderBy/ThenBy expressions for the sort keys' property "
        + "types at runtime, as any LINQ provider query does.";

    /// <summary>
    /// Sorts the query by <paramref name="sortingParams"/> and loads one page: a <c>COUNT</c>,
    /// then the page with <c>OFFSET</c>/<c>LIMIT</c> (skipped when the count is <c>0</c>). For
    /// <see cref="PagingParams.All"/>, one query loads every item.
    /// </summary>
    /// <example>
    /// <code>
    /// PagedData&lt;OrderDto&gt; page = await db.Orders
    ///     .Where(order => order.CustomerId == customerId)
    ///     .Select(order => new OrderDto(order.Id, order.Customer.Name, order.CreatedAt))
    ///     .PaginateAsync(query.GetSortingParams(), query.GetPagingParams(), cancellationToken);
    /// </code>
    /// </example>
    /// <typeparam name="TSource">The item type the sort keys name properties of.</typeparam>
    /// <param name="source">The query, filtered and projected but not sorted.</param>
    /// <param name="sortingParams">The sorting.</param>
    /// <param name="pagingParams">The page to load.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>The page.</returns>
    /// <exception cref="SortingParamNotFoundException">
    /// A sort key names no property of <typeparamref name="TSource"/>.
    /// </exception>
    [RequiresUnreferencedCode(QuerySortWarning)]
    [RequiresDynamicCode(QuerySortWarning)]
    public static async Task<PagedData<TSource>> PaginateAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TSource>(
        this IQueryable<TSource> source,
        SortingParams sortingParams,
        PagingParams pagingParams,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(pagingParams);

        IQueryable<TSource> sorted = source.OrderBy(sortingParams);
        if (pagingParams.IsAll)
        {
            TSource[] all = await sorted.ToArrayAsync(cancellationToken).ConfigureAwait(false);
            return new PagedData<TSource>(all, all.Length, pagingParams);
        }

        int count = await source.CountAsync(cancellationToken).ConfigureAwait(false);
        if (count == 0)
        {
            return PagedData<TSource>.Empty(pagingParams);
        }

        TSource[] page = await sorted
            .Skip(pagingParams.Skip)
            .Take(pagingParams.PerPage)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        return new PagedData<TSource>(page, count, pagingParams);
    }
}
