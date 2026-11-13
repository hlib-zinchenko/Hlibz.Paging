using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;

using Hlibz.Paging.Internal;

namespace Hlibz.Paging;

/// <summary>
/// What a DTO lets clients sort by: its <see cref="SortableAttribute"/> properties and its
/// default order. Parses a client's sort string against them, so a client can only sort by what
/// the DTO allows.
/// </summary>
/// <example>
/// <code>
/// // GET /orders?sort=customer_name,created_at:desc
/// if (!SortableDto.For&lt;OrderDto&gt;().TryParse(sort, out SortingParams? sorting, out string? error))
/// {
///     return Results.ValidationProblem(new Dictionary&lt;string, string[]&gt; { ["sort"] = [error] });
/// }
/// </code>
/// </example>
public sealed class SortableDto
{
    /// <summary>Why checking types given at runtime isn't trimming-safe.</summary>
    private const string TypesWarning =
        "Reads the public properties of each type given, which trimming may remove; "
        + "SortableDto.For<TDto>() keeps them.";

    private static readonly ConcurrentDictionary<Type, SortableDto> Cache = new();

    private readonly string[] _properties;

    /// <summary>
    /// The declared names by normalized name (<see cref="PropertyLookup.Normalize"/>), any case;
    /// the first of two that normalize alike wins.
    /// </summary>
    private readonly Dictionary<string, string> _byNormalizedName = new(StringComparer.OrdinalIgnoreCase);

    private SortableDto(Type dtoType, string[] properties, SortingParam defaultSorting)
    {
        DtoType = dtoType;
        _properties = properties;
        DefaultSorting = defaultSorting;
        foreach (string property in properties)
        {
            _byNormalizedName.TryAdd(PropertyLookup.Normalize(property), property);
        }
    }

    /// <summary>
    /// Gets the DTO.
    /// </summary>
    public Type DtoType { get; }

    /// <summary>
    /// Gets the names of the properties clients may sort by, as declared.
    /// </summary>
    public IReadOnlyList<string> Properties => _properties;

    /// <summary>
    /// Gets the order of a request that names no sort key: the
    /// <see cref="SortableAttribute.IsDefault"/> property.
    /// </summary>
    public SortingParam DefaultSorting { get; }

    /// <summary>
    /// What <typeparamref name="TDto"/> lets clients sort by.
    /// </summary>
    /// <typeparam name="TDto">The DTO.</typeparam>
    /// <returns>Its sortable properties and default order.</returns>
    /// <exception cref="InvalidOperationException">Not exactly one of its sortable properties is
    /// the default.</exception>
    public static SortableDto For<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TDto>() => For(typeof(TDto));

    /// <summary>
    /// What <paramref name="dtoType"/> lets clients sort by.
    /// </summary>
    /// <param name="dtoType">The DTO.</param>
    /// <returns>Its sortable properties and default order.</returns>
    /// <exception cref="InvalidOperationException">Not exactly one of its sortable properties is
    /// the default.</exception>
    public static SortableDto For([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type dtoType)
    {
        ArgumentNullException.ThrowIfNull(dtoType);
        return Cache.TryGetValue(dtoType, out SortableDto? cached)
            ? cached
            : Cache.GetOrAdd(dtoType, Build(dtoType));
    }

    /// <summary>
    /// Checks every DTO in <paramref name="assemblies"/> with a <see cref="SortableAttribute"/>
    /// property, at startup, so a DTO without exactly one default stops the app instead of
    /// failing the first request that sorts it. Abstract types are skipped: a base class may mark
    /// properties sortable for the DTOs that inherit them. To check only the DTOs your lists
    /// sort, pass them to <see cref="ValidateAll(IEnumerable{Type})"/> instead.
    /// </summary>
    /// <remarks>
    /// The package's source generator lists each assembly's DTOs when it compiles (and reports an
    /// invalid one as build error <c>HPAG001</c>), so nothing is scanned at runtime and this is
    /// trimming-safe. An assembly that doesn't reference Hlibz.Paging has no DTOs to list. A DTO
    /// the generated list can't name (a private nested type) is checked only by the analyzer.
    /// </remarks>
    /// <example>
    /// <code>
    /// SortableDto.ValidateAll([typeof(OrderDto).Assembly]);
    /// </code>
    /// </example>
    /// <param name="assemblies">The assemblies with the DTOs.</param>
    /// <exception cref="InvalidOperationException">One or more DTOs are invalid, the message
    /// naming each; or an assembly that references Hlibz.Paging wasn't compiled with its source
    /// generator.</exception>
    public static void ValidateAll(IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        Validate(assemblies.Distinct().SelectMany(Manifest));
    }

    /// <summary>
    /// Checks each of <paramref name="dtoTypes"/>, at startup, so a DTO without exactly one
    /// default stops the app instead of failing the first request that sorts it. Use it rather
    /// than <see cref="ValidateAll(IEnumerable{Assembly})"/> when some types with
    /// <see cref="SortableAttribute"/> properties are never sorted, such as a DTO that only
    /// inherits them.
    /// </summary>
    /// <example>
    /// <code>
    /// SortableDto.ValidateAll([typeof(OrderDto), typeof(CustomerDto)]);
    /// </code>
    /// </example>
    /// <param name="dtoTypes">The DTOs your lists sort.</param>
    /// <exception cref="InvalidOperationException">One or more DTOs are invalid; the message names
    /// each.</exception>
    [RequiresUnreferencedCode(TypesWarning)]
    public static void ValidateAll(IEnumerable<Type> dtoTypes)
    {
        ArgumentNullException.ThrowIfNull(dtoTypes);
        Validate(dtoTypes.Select(type => (Func<SortableDto>)(() => For(type))));
    }

    /// <summary>
    /// Parses a client's sort string: comma-separated keys, each a sortable property in any
    /// casing (<c>created_at</c>, <c>createdAt</c>, <c>CreatedAt</c>, <c>created-at</c>), optionally
    /// followed by <c>:asc</c> (the default) or <c>:desc</c>. An empty string is
    /// <see cref="DefaultSorting"/>. Errors name properties as declared.
    /// </summary>
    /// <param name="sort">The sort string, e.g. <c>customer_name,created_at:desc</c>.</param>
    /// <param name="sortingParams">The sorting, keys naming the DTO's properties as declared.</param>
    /// <param name="error">Why <paramref name="sort"/> was rejected, for the client.</param>
    /// <returns>Whether <paramref name="sort"/> is valid.</returns>
    public bool TryParse(
        string? sort,
        [NotNullWhen(true)] out SortingParams? sortingParams,
        [NotNullWhen(false)] out string? error) =>
        TryParse(sort, null, out sortingParams, out error);

    /// <summary>
    /// Parses a client's sort string, as <see cref="TryParse(string?, out SortingParams?, out string?)"/>
    /// does, with errors naming properties as <paramref name="namingPolicy"/> does: the casing your
    /// clients see.
    /// </summary>
    /// <param name="sort">The sort string, e.g. <c>customer_name,created_at:desc</c>.</param>
    /// <param name="namingPolicy">How errors name properties, e.g.
    /// <see cref="JsonNamingPolicy.SnakeCaseLower"/>; <see langword="null"/> for as declared.</param>
    /// <param name="sortingParams">The sorting, keys naming the DTO's properties as declared.</param>
    /// <param name="error">Why <paramref name="sort"/> was rejected, for the client.</param>
    /// <returns>Whether <paramref name="sort"/> is valid.</returns>
    public bool TryParse(
        string? sort,
        JsonNamingPolicy? namingPolicy,
        [NotNullWhen(true)] out SortingParams? sortingParams,
        [NotNullWhen(false)] out string? error)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            sortingParams = SortingParams.Create(DefaultSorting);
            error = null;
            return true;
        }

        return SortStringParser.TryParse(
            sort,
            sortBy => _byNormalizedName.GetValueOrDefault(PropertyLookup.Normalize(sortBy)),
            () => [.. _properties.Select(property => namingPolicy?.ConvertName(property) ?? property)],
            out sortingParams,
            out error);
    }

    private static SortableDto Build([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type dtoType)
    {
        (PropertyInfo Property, SortableAttribute? Attribute)[] properties =
        [
            .. dtoType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(property => (property, property.GetCustomAttribute<SortableAttribute>())),
        ];
        (PropertyInfo Property, SortableAttribute? Attribute)[] defaults =
            [.. properties.Where(pair => pair.Attribute is { IsDefault: true })];
        if (defaults.Length != 1)
        {
            throw new InvalidOperationException(
                $"{dtoType.FullName} must have exactly one property marked with "
                + $"[Sortable(IsDefault = true)], found {defaults.Length}.");
        }

        return new SortableDto(
            dtoType,
            [.. properties.Where(pair => pair.Attribute is not null).Select(pair => pair.Property.Name)],
            SortingParam.Create(defaults[0].Property.Name, defaults[0].Attribute!.DefaultDirection));
    }

    private static void Validate(IEnumerable<Func<SortableDto>> dtos)
    {
        List<string> problems = [];
        foreach (Func<SortableDto> dto in dtos)
        {
            try
            {
                dto();
            }
            catch (InvalidOperationException exception)
            {
                problems.Add(exception.Message);
            }
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, problems));
        }
    }

    /// <summary>
    /// The DTOs the source generator listed in <paramref name="assembly"/>: none for an assembly
    /// that doesn't reference Hlibz.Paging (or is Hlibz.Paging), which can't declare any.
    /// </summary>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Trimming removes only references nothing uses; an assembly that uses no Hlibz.Paging type declares no DTOs.")]
    private static IReadOnlyList<Func<SortableDto>> Manifest(Assembly assembly)
    {
        if (assembly.GetCustomAttribute<SortableDtoManifestAttribute>() is { } manifest)
        {
            return manifest.Dtos;
        }

        Assembly paging = typeof(SortableDto).Assembly;
        bool referencesPaging = assembly.GetReferencedAssemblies().Any(name =>
            AssemblyName.ReferenceMatchesDefinition(name, paging.GetName()));
        return assembly == paging || !referencesPaging
            ? []
            : throw new InvalidOperationException(
                $"{assembly.GetName().Name} has no list of sortable DTOs: the Hlibz.Paging source "
                + "generator didn't run when it was compiled. Reference the Hlibz.Paging package "
                + "from that project, and don't exclude its analyzers.");
    }
}
