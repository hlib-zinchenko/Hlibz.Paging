namespace Hlibz.Paging;

/// <summary>
/// The page a client asked for: a 1-based page number and a page size. A missing or
/// out-of-range value falls back to its default rather than failing.
/// </summary>
public sealed class PagingParams
{
    /// <summary>
    /// The page used when none (or one below 1) is given.
    /// </summary>
    public const int DefaultPage = 1;

    /// <summary>
    /// The page size used when none (or one below 1) is given.
    /// </summary>
    public const int DefaultPerPage = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="PagingParams"/> class.
    /// </summary>
    /// <param name="page">The 1-based page; <see cref="DefaultPage"/> when missing or below 1.</param>
    /// <param name="perPage">The page size; <see cref="DefaultPerPage"/> when missing or below 1.</param>
    public PagingParams(int? page, int? perPage)
    {
        Page = page is null or < 1 ? DefaultPage : page.Value;
        PerPage = perPage is null or < 1 ? DefaultPerPage : perPage.Value;
    }

    /// <summary>
    /// Gets a single page holding every item.
    /// </summary>
    public static PagingParams All { get; } = new(1, int.MaxValue);

    /// <summary>
    /// Gets the 1-based page number.
    /// </summary>
    public int Page { get; }

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PerPage { get; }

    /// <summary>
    /// Gets a value indicating whether this is <see cref="All"/>: one page with every item.
    /// </summary>
    public bool IsAll => Page == 1 && PerPage == int.MaxValue;

    /// <summary>
    /// Gets the number of items before this page, capped at <see cref="int.MaxValue"/> so a huge
    /// page number gives an empty page rather than an overflow.
    /// </summary>
    public int Skip => (int)Math.Min((Page - 1L) * PerPage, int.MaxValue);
}
