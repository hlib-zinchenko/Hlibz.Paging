namespace Hlibz.Paging;

/// <summary>
/// The sort keys of a query, in order: the first decides, each next one breaks ties.
/// </summary>
/// <example>
/// <code>
/// SortingParams sorting = SortingParams.Create().Descending("CreatedAt").Ascending("Id");
/// </code>
/// </example>
public sealed class SortingParams
{
    private readonly List<SortingParam> _sortingParams = [];

    private SortingParams()
    {
    }

    /// <summary>
    /// Gets the sort keys, in order.
    /// </summary>
    public IReadOnlyList<SortingParam> Params => _sortingParams.AsReadOnly();

    /// <summary>
    /// Starts with no sort keys. With none added, <c>OrderBy</c> sorts by an <c>Id</c> property.
    /// </summary>
    /// <returns>The sorting.</returns>
    public static SortingParams Create() => new();

    /// <summary>
    /// Starts with <paramref name="sortingParam"/> as the first sort key.
    /// </summary>
    /// <param name="sortingParam">The first sort key.</param>
    /// <returns>The sorting.</returns>
    public static SortingParams Create(SortingParam sortingParam)
    {
        ArgumentNullException.ThrowIfNull(sortingParam);
        SortingParams sorting = new();
        sorting._sortingParams.Add(sortingParam);
        return sorting;
    }

    /// <summary>
    /// Adds <paramref name="property"/>, smallest first, as the next sort key.
    /// </summary>
    /// <param name="property">The property to sort by.</param>
    /// <returns>This sorting.</returns>
    public SortingParams Ascending(string property)
    {
        _sortingParams.Add(SortingParam.Ascending(property));
        return this;
    }

    /// <summary>
    /// Adds <paramref name="property"/>, largest first, as the next sort key.
    /// </summary>
    /// <param name="property">The property to sort by.</param>
    /// <returns>This sorting.</returns>
    public SortingParams Descending(string property)
    {
        _sortingParams.Add(SortingParam.Descending(property));
        return this;
    }
}
