using Microsoft.CodeAnalysis;

namespace Hlibz.Paging.Generators;

/// <summary>
/// Reads a type's <c>[Sortable]</c> properties from its symbol the way <c>SortableDto</c> reads
/// them at runtime: every public instance property, inherited ones included, the most derived
/// declaration of each name, its <c>[Sortable]</c> inherited from the property it overrides.
/// </summary>
internal static class SortableDtoSymbols
{
    /// <summary>The metadata name of <c>Hlibz.Paging.SortableAttribute</c>.</summary>
    public const string SortableAttributeName = "Hlibz.Paging.SortableAttribute";

    /// <summary>
    /// Whether <c>SortableDto</c> treats <paramref name="type"/> as a DTO to check: a class or
    /// struct, not abstract, not generic or nested in a generic type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns><see langword="true"/> when it is one.</returns>
    public static bool IsCandidate(INamedTypeSymbol type)
    {
        if (type.TypeKind is not (TypeKind.Class or TypeKind.Struct) || type.IsAbstract || type.IsStatic)
        {
            return false;
        }

        for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
        {
            if (current.IsGenericType)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Counts the type's <c>[Sortable]</c> properties and how many of them are the default.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="sortable">The <c>SortableAttribute</c> symbol.</param>
    /// <returns>The number of sortable properties, and of defaults among them.</returns>
    public static (int Sortable, int Defaults) Count(INamedTypeSymbol type, INamedTypeSymbol sortable)
    {
        int sortableCount = 0;
        int defaults = 0;
        HashSet<string> seen = new(StringComparer.Ordinal);
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            foreach (IPropertySymbol property in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (property.IsStatic
                    || property.DeclaredAccessibility != Accessibility.Public
                    || !seen.Add(property.MetadataName)
                    || FindSortable(property, sortable) is not { } attribute)
                {
                    continue;
                }

                sortableCount++;
                if (IsDefault(attribute))
                {
                    defaults++;
                }
            }
        }

        return (sortableCount, defaults);
    }

    private static AttributeData? FindSortable(IPropertySymbol property, INamedTypeSymbol sortable)
    {
        for (IPropertySymbol? current = property; current is not null; current = current.OverriddenProperty)
        {
            AttributeData? attribute = current.GetAttributes().FirstOrDefault(
                candidate => SymbolEqualityComparer.Default.Equals(candidate.AttributeClass, sortable));
            if (attribute is not null)
            {
                return attribute;
            }
        }

        return null;
    }

    private static bool IsDefault(AttributeData attribute)
    {
        foreach (KeyValuePair<string, TypedConstant> argument in attribute.NamedArguments)
        {
            if (argument.Key == "IsDefault" && argument.Value.Value is bool isDefault)
            {
                return isDefault;
            }
        }

        return false;
    }
}
