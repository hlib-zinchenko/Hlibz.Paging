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
    /// case-insensitively, then by the <c>Id</c> property when <typeparamref name="T"/> has one
    /// and no key names it, so items with equal keys keep one order and pages neither repeat nor
    /// skip them. With no sort keys, sorts by <c>Id</c>.
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

        List<(PropertyInfo Property, ListSortDirection Direction)> sortBy = SortPlan.For<T>(sortingParams);

        // In memory a string property's null would throw on ToUpper(); a database query keeps
        // plain UPPER(column), where null stays null.
        bool inMemory = source.Provider is EnumerableQuery;
        Expression query = source.Expression;
        for (int index = 0; index < sortBy.Count; index++)
        {
            ParameterExpression item = Expression.Parameter(typeof(T), "item");
            Expression key = SortKey(Expression.Property(item, sortBy[index].Property), inMemory);
            query = Expression.Call(
                typeof(Queryable),
                MethodName(index == 0, sortBy[index].Direction),
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
        ArgumentNullException.ThrowIfNull(sortingParams);
        ArgumentNullException.ThrowIfNull(pagingParams);

        // Sorted with delegates rather than through AsQueryable(), which would compile the sort
        // expression on every call.
        T[] sorted = [.. SortPlan.Sort(source, SortPlan.For<T>(sortingParams))];
        T[] page = pagingParams.IsAll
            ? sorted
            : [.. sorted.Skip(pagingParams.Skip).Take(pagingParams.PerPage)];
        return new PagedData<T>(page, sorted.Length, pagingParams);
    }

    private static Expression SortKey(MemberExpression property, bool inMemory)
    {
        if (property.Type != typeof(string))
        {
            return property;
        }

        Expression upper = Expression.Call(property, ToUpper);
        if (!inMemory)
        {
            return upper;
        }

        ConstantExpression none = Expression.Constant(null, typeof(string));
        return Expression.Condition(Expression.Equal(property, none), none, upper);
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
