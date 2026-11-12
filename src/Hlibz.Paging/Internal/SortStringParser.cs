using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Hlibz.Paging.Internal;

/// <summary>
/// Parses a client's sort string: comma-separated keys, each a property optionally followed by
/// <c>:asc</c> or <c>:desc</c> (<c>name,created_at:desc</c>).
/// </summary>
internal static partial class SortStringParser
{
    /// <summary>
    /// Parses <paramref name="sort"/>, accepting only the properties
    /// <paramref name="findProperty"/> finds.
    /// </summary>
    /// <param name="sort">The sort string, not empty.</param>
    /// <param name="findProperty">The declared name of the sortable property a key names, or
    /// <see langword="null"/>.</param>
    /// <param name="allowed">The sortable properties as the client sees them, for errors.</param>
    /// <param name="sortingParams">The sorting, keys naming properties as declared.</param>
    /// <param name="error">Why <paramref name="sort"/> was rejected, for the client.</param>
    /// <returns>Whether <paramref name="sort"/> is valid.</returns>
    public static bool TryParse(
        string sort,
        Func<string, string?> findProperty,
        IReadOnlyList<string> allowed,
        [NotNullWhen(true)] out SortingParams? sortingParams,
        [NotNullWhen(false)] out string? error)
    {
        sortingParams = null;
        SortingParams parsed = SortingParams.Create();
        List<string> unsupported = [];
        foreach (string key in sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            Match match = SortKey().Match(key);
            if (!match.Success)
            {
                error = $"Invalid sort key '{key}': use property, property:asc or property:desc.";
                return false;
            }

            string sortBy = match.Groups["property"].Value;
            if (findProperty(sortBy) is not { } property)
            {
                unsupported.Add(sortBy);
            }
            else if (match.Groups["direction"].Value.Equals("desc", StringComparison.OrdinalIgnoreCase))
            {
                parsed.Descending(property);
            }
            else
            {
                parsed.Ascending(property);
            }
        }

        if (unsupported.Count > 0)
        {
            error = $"Can't sort by {string.Join(", ", unsupported)}. Sort by: {string.Join(", ", allowed)}.";
            return false;
        }

        if (parsed.Params.Count == 0)
        {
            error = "The sort string names no sort key.";
            return false;
        }

        sortingParams = parsed;
        error = null;
        return true;
    }

    [GeneratedRegex(
        "^(?<property>[A-Za-z0-9_-]+)(?::(?<direction>asc|desc))?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex SortKey();
}
