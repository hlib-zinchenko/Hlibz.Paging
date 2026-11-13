using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace Hlibz.Paging.Generators.Tests;

public sealed class SortableDtoAnalyzerTests
{
    [Fact]
    public async Task Analyze_WithOneDefault_ReportsNothing()
    {
        ImmutableArray<Diagnostic> diagnostics = await Compiler.AnalyzeAsync("""
            namespace App;

            public sealed class OrderDto
            {
                [Sortable]
                public int Id { get; init; }

                [Sortable(IsDefault = true, DefaultDirection = ListSortDirection.Descending)]
                public DateTime CreatedAt { get; init; }
            }

            public sealed class PlainDto
            {
                public int Id { get; init; }
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Analyze_WithoutDefault_ReportsHpag001OnTheType()
    {
        ImmutableArray<Diagnostic> diagnostics = await Compiler.AnalyzeAsync("""
            namespace App;

            public sealed class NoDefaultDto
            {
                [Sortable]
                public int Id { get; init; }
            }
            """);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("HPAG001", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(
            "App.NoDefaultDto must have exactly one property marked with [Sortable(IsDefault = true)], found 0",
            diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture));
        Microsoft.CodeAnalysis.Text.SourceText text =
            await diagnostic.Location.SourceTree!.GetTextAsync(TestContext.Current.CancellationToken);
        Assert.Equal("NoDefaultDto", text.ToString(diagnostic.Location.SourceSpan));
    }

    [Fact]
    public async Task Analyze_WithTwoDefaults_ReportsHowMany()
    {
        ImmutableArray<Diagnostic> diagnostics = await Compiler.AnalyzeAsync("""
            namespace App;

            public readonly record struct TwoDefaults(
                [property: Sortable(IsDefault = true)] int Id,
                [property: Sortable(IsDefault = true)] string Name);
            """);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.EndsWith("found 2", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Analyze_WithDefaultOnBaseClass_ChecksTheDerivedDtoOnly()
    {
        ImmutableArray<Diagnostic> diagnostics = await Compiler.AnalyzeAsync("""
            namespace App;

            public abstract class AuditedDto
            {
                [Sortable(IsDefault = true)]
                public virtual DateTime UpdatedAt { get; init; }
            }

            public sealed class AuditedOrderDto : AuditedDto
            {
                [Sortable]
                public int Id { get; init; }
            }

            public sealed class OverridingDto : AuditedDto
            {
                public override DateTime UpdatedAt { get; init; }
            }

            public abstract class NoDefaultBase
            {
                [Sortable]
                public int Id { get; init; }
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Analyze_WithGenericOrNonPublicProperty_IgnoresIt()
    {
        ImmutableArray<Diagnostic> diagnostics = await Compiler.AnalyzeAsync("""
            namespace App;

            public sealed class OpenRow<T>
            {
                [Sortable]
                public T? Value { get; init; }
            }

            public sealed class HiddenDto
            {
                [Sortable]
                internal int Id { get; init; }
            }
            """);

        Assert.Empty(diagnostics);
    }
}
