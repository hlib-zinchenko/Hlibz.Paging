using System.Diagnostics.CodeAnalysis;

namespace Hlibz.Paging;

/// <summary>
/// Thrown when a sort key names no public property of the sorted type: a bug in the query, not
/// a client error (a client's sort keys are checked against the DTO's <see cref="SortableAttribute"/>
/// properties before they get here).
/// </summary>
[SuppressMessage(
    "Design",
    "CA1032:Implement standard exception constructors",
    Justification = "Always thrown with the missing property names.")]
public sealed class SortingParamNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SortingParamNotFoundException"/> class.
    /// </summary>
    /// <param name="type">The sorted type.</param>
    /// <param name="propertyNames">The sort keys that name no property.</param>
    public SortingParamNotFoundException(Type type, IReadOnlyList<string> propertyNames)
        : base($"{type?.Name} has no property to sort by named: {string.Join(", ", propertyNames ?? [])}.")
    {
        PropertyNames = propertyNames ?? [];
    }

    /// <summary>
    /// Gets the sort keys that name no property.
    /// </summary>
    public IReadOnlyList<string> PropertyNames { get; }
}
