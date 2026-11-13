using System.ComponentModel;

namespace Hlibz.Paging;

/// <summary>
/// The sortable DTOs of an assembly, written into it by the package's source generator when it
/// compiles, so <see cref="SortableDto.ValidateAll(IEnumerable{System.Reflection.Assembly})"/>
/// reads a list rather than scanning every type. Not for use in your own code.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class SortableDtoManifestAttribute : Attribute
{
    /// <summary>
    /// Gets each DTO's <see cref="SortableDto.For{TDto}"/>, which throws for an invalid one.
    /// </summary>
    public abstract IReadOnlyList<Func<SortableDto>> Dtos { get; }
}
