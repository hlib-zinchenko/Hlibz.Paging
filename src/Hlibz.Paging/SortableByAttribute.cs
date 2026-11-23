using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hlibz.Paging;

/// <summary>
/// Validates a sort string (<c>customer_name,created_at:desc</c>) against
/// <see cref="DtoType"/>'s <see cref="SortableAttribute"/> properties, as
/// <see cref="SortableDto.TryParse(string?, JsonNamingPolicy?, out SortingParams?, out string?)"/>
/// does: a DataAnnotations rule, so whatever validates the query or request it sits on (minimal
/// APIs' <c>AddValidation()</c>, MVC, <see cref="Validator"/>) rejects a bad sort before it reaches
/// your code. A <see langword="null"/> or empty sort string is valid: the DTO's default order.
/// </summary>
/// <remarks>
/// The error is the parser's, written for the client and naming what can be sorted by in
/// <see cref="NamingPolicy"/>'s casing, unless you set <see cref="ValidationAttribute.ErrorMessage"/>
/// (or a resource) of your own. Once validated, turn the string into sorting with
/// <see cref="SortableDto.Parse(string?)"/>.
/// </remarks>
/// <example>
/// <code>
/// public sealed record GetOrdersQuery(
///     [property: SortableBy(typeof(OrderDto), NamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
///     string? Sort);
///
/// SortingParams sorting = SortableDto.For&lt;OrderDto&gt;().Parse(query.Sort);
/// </code>
/// </example>
/// <param name="dtoType">The DTO the list returns.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Field)]
public sealed class SortableByAttribute(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type dtoType)
    : ValidationAttribute
{
    /// <summary>Gets the DTO the list returns, whose sortable properties the sort string may name.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    public Type DtoType { get; } = dtoType ?? throw new ArgumentNullException(nameof(dtoType));

    /// <summary>
    /// Gets or sets how errors name the DTO's properties: the casing your clients see, such as
    /// <see cref="JsonKnownNamingPolicy.SnakeCaseLower"/>. The default,
    /// <see cref="JsonKnownNamingPolicy.Unspecified"/>, names them as declared. Keys are accepted in
    /// any casing either way.
    /// </summary>
    public JsonKnownNamingPolicy NamingPolicy { get; set; }

    /// <summary>
    /// Names a property of <see cref="DtoType"/> as <see cref="NamingPolicy"/> does: how errors,
    /// and documentation of the parameter, name it.
    /// </summary>
    /// <param name="propertyName">The property's declared name, from
    /// <see cref="SortableDto.Properties"/> or <see cref="SortableDto.DefaultSorting"/>.</param>
    /// <returns>The name clients see.</returns>
    /// <exception cref="NotSupportedException"><see cref="NamingPolicy"/> is a value this
    /// version doesn't know.</exception>
    public string ConvertName(string propertyName)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        return JsonPolicy()?.ConvertName(propertyName) ?? propertyName;
    }

    /// <inheritdoc/>
    public override bool IsValid(object? value) => Check(value, memberName: null) is null;

    /// <inheritdoc/>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);
        string? error = Check(value, validationContext.MemberName ?? validationContext.DisplayName);
        if (error is null)
        {
            return ValidationResult.Success;
        }

        bool customMessage = ErrorMessage is not null || ErrorMessageResourceName is not null;
        string[]? members = validationContext.MemberName is { } member ? [member] : null;
        return new ValidationResult(
            customMessage ? FormatErrorMessage(validationContext.DisplayName) : error,
            members);
    }

    /// <summary>Why <paramref name="value"/> is invalid; <see langword="null"/> when it isn't.</summary>
    private string? Check(object? value, string? memberName)
    {
        if (value is null)
        {
            return null;
        }

        if (value is not string sort)
        {
            throw new InvalidOperationException(
                $"[SortableBy] validates a string, but {memberName ?? "the value"} is a "
                + $"{value.GetType().FullName}.");
        }

        return SortableDto.For(DtoType).TryParse(sort, JsonPolicy(), out _, out string? error)
            ? null
            : error;
    }

    private JsonNamingPolicy? JsonPolicy() => NamingPolicy switch
    {
        JsonKnownNamingPolicy.Unspecified => null,
        JsonKnownNamingPolicy.CamelCase => JsonNamingPolicy.CamelCase,
        JsonKnownNamingPolicy.SnakeCaseLower => JsonNamingPolicy.SnakeCaseLower,
        JsonKnownNamingPolicy.SnakeCaseUpper => JsonNamingPolicy.SnakeCaseUpper,
        JsonKnownNamingPolicy.KebabCaseLower => JsonNamingPolicy.KebabCaseLower,
        JsonKnownNamingPolicy.KebabCaseUpper => JsonNamingPolicy.KebabCaseUpper,
        _ => throw new NotSupportedException($"Naming policy {NamingPolicy} isn't supported."),
    };
}
