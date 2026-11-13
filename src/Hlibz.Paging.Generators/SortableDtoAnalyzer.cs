using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Hlibz.Paging.Generators;

/// <summary>
/// Reports a DTO whose <c>[Sortable]</c> properties don't include exactly one default
/// (<c>HPAG001</c>): the check <c>SortableDto</c> makes at runtime, as a build error.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SortableDtoAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// HPAG001: the DTO's <c>[Sortable]</c> properties don't include exactly one default.
    /// </summary>
    internal static readonly DiagnosticDescriptor NotOneDefault = new(
        "HPAG001",
        "Sortable DTO doesn't have exactly one default",
        "{0} must have exactly one property marked with [Sortable(IsDefault = true)], found {1}",
        "Hlibz.Paging",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A request that names no sort key is sorted by the default property, so a DTO with [Sortable] properties needs exactly one with IsDefault = true.",
        helpLinkUri: "https://github.com/hlib-zinchenko/Hlibz.Paging#build-errors");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(NotOneDefault);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            if (start.Compilation.GetTypeByMetadataName(SortableDtoSymbols.SortableAttributeName)
                is not { } sortable)
            {
                return;
            }

            start.RegisterSymbolAction(
                symbolContext => Analyze(symbolContext, sortable),
                SymbolKind.NamedType);
        });
    }

    private static void Analyze(SymbolAnalysisContext context, INamedTypeSymbol sortable)
    {
        INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;
        if (!SortableDtoSymbols.IsCandidate(type))
        {
            return;
        }

        (int sortableCount, int defaults) = SortableDtoSymbols.Count(type, sortable);
        if (sortableCount == 0 || defaults == 1)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            NotOneDefault,
            type.Locations.FirstOrDefault(location => location.IsInSource),
            type.ToDisplayString(),
            defaults.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }
}
