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

    /// <summary>Why scanning assemblies isn't trimming-safe.</summary>
    private const string ScanWarning =
        "Scans every type of each assembly, which trimming may remove.";

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
    /// <example>
    /// <code>
    /// SortableDto.ValidateAll([typeof(OrderDto).Assembly]);
    /// </code>
    /// </example>
    /// <param name="assemblies">The assemblies with the DTOs.</param>
    /// <exception cref="InvalidOperationException">One or more DTOs are invalid; the message names
    /// each.</exception>
    [RequiresUnreferencedCode(ScanWarning)]
    public static void ValidateAll(IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        Validate(assemblies.SelectMany(assembly => assembly.GetTypes()).Where(IsSortable));
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
        Validate(dtoTypes);
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

    [RequiresUnreferencedCode(TypesWarning)]
    private static void Validate(IEnumerable<Type> dtoTypes)
    {
        List<string> problems = [];
        foreach (Type type in dtoTypes)
        {
            try
            {
                For(type);
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

    [RequiresUnreferencedCode(ScanWarning)]
    private static bool IsSortable(Type type) =>
        !type.IsAbstract
        && !type.ContainsGenericParameters
        && type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Any(property => property.IsDefined(typeof(SortableAttribute)));
}
