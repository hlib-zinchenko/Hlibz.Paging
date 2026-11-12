using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

using Hlibz.Paging.Internal;

namespace Hlibz.Paging;

/// <summary>
/// Applies <see cref="SortingParams"/> to a query. Plain LINQ expressions, so the same sorting
/// runs in the database (<c>PaginateAsync</c> in Hlibz.Paging.EntityFrameworkCore) and in memory
/// (<see cref="Paginate{T}"/>).
/// </summary>
public static class SortingExtensions
{
    private static readonly MethodInfo ToUpper = typeof(string).GetMethod(
        nameof(string.ToUpper),
        Type.EmptyTypes)!;

    /// <summary>
    /// Sorts by each sort key in turn (<c>OrderBy</c>, then <c>ThenBy</c>), strings
    /// case-insensitively. With no sort keys, sorts by the <c>Id</c> property.
    /// </summary>
    /// <typeparam name="T">The item type the sort keys name properties of.</typeparam>
    /// <param name="source">The query.</param>
    /// <param name="sortingParams">The sorting.</param>
    /// <returns>The sorted query.</returns>
    /// <exception cref="SortingParamNotFoundException">
    /// A sort key (or, with none, <c>Id</c>) names no public property of <typeparamref name="T"/>.
    /// </exception>
    public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> source, SortingParams sortingParams)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(sortingParams);

        IReadOnlyList<SortingParam> keys = sortingParams.Params.Count > 0
            ? sortingParams.Params
            : [SortingParam.Ascending("Id")];
        Expression query = source.Expression;
        for (int index = 0; index < keys.Count; index++)
        {
            PropertyInfo property = PropertyLookup.Find(typeof(T), keys, index);
            ParameterExpression item = Expression.Parameter(typeof(T), "item");
            Expression key = Expression.Property(item, property);
            if (property.PropertyType == typeof(string))
            {
                key = Expression.Call(key, ToUpper);
            }

            query = Expression.Call(
                typeof(Queryable),
                MethodName(index == 0, keys[index].SortDirection),
                [typeof(T), key.Type],
                query,
                Expression.Quote(Expression.Lambda(key, item)));
        }

        return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(query);
    }

    /// <summary>
    /// Sorts, then pages, an in-memory sequence: for lists whose sortable values are computed
    /// after loading rather than stored.
    /// </summary>
    /// <typeparam name="T">The item type the sort keys name properties of.</typeparam>
    /// <param name="source">The items.</param>
    /// <param name="sortingParams">The sorting.</param>
    /// <param name="pagingParams">The page to return.</param>
    /// <returns>The page.</returns>
    public static PagedData<T> Paginate<T>(
        this IEnumerable<T> source,
        SortingParams sortingParams,
        PagingParams pagingParams)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(pagingParams);

        T[] sorted = [.. source.AsQueryable().OrderBy(sortingParams)];
        T[] page = pagingParams.IsAll
            ? sorted
            : [.. sorted.Skip(pagingParams.Skip).Take(pagingParams.PerPage)];
        return new PagedData<T>(page, sorted.Length, pagingParams);
    }

    private static string MethodName(bool first, ListSortDirection direction) =>
        (first, direction) switch
        {
            (true, ListSortDirection.Descending) => nameof(Queryable.OrderByDescending),
            (true, _) => nameof(Queryable.OrderBy),
            (false, ListSortDirection.Descending) => nameof(Queryable.ThenByDescending),
            (false, _) => nameof(Queryable.ThenBy),
        };
}
