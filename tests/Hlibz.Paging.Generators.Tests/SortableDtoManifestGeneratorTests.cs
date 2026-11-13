using System.Reflection;

using Microsoft.CodeAnalysis;

namespace Hlibz.Paging.Generators.Tests;

public sealed class SortableDtoManifestGeneratorTests
{
    private const string Dtos = """
        namespace App;

        public sealed class OrderDto
        {
            [Sortable(IsDefault = true)]
            public int Id { get; init; }
        }

        internal readonly record struct CityRow([property: Sortable(IsDefault = true)] string Name);

        #pragma warning disable HPAG001
        public sealed class NoDefaultDto
        {
            [Sortable]
            public int Id { get; init; }
        }
        #pragma warning restore HPAG001

        public abstract class AuditedDto
        {
            [Sortable(IsDefault = true)]
            public DateTime UpdatedAt { get; init; }
        }

        public sealed class InheritedDto : AuditedDto
        {
        }

        public sealed class OpenRow<T>
        {
            [Sortable(IsDefault = true)]
            public T? Value { get; init; }
        }

        public sealed class Outer
        {
            private sealed class PrivateDto
            {
                [Sortable(IsDefault = true)]
                public int Id { get; init; }
            }
        }

        public sealed class PlainDto
        {
            public int Id { get; init; }
        }
        """;

    [Fact]
    public void Generate_WithDtos_ListsEveryNamedSortableDto()
    {
        (string? manifest, _) = Compiler.Generate(Dtos);

        Assert.NotNull(manifest);
        string[] listed =
        [
            .. manifest.Split('\n')
                .Where(line => line.Contains("SortableDto.For<", StringComparison.Ordinal))
                .Select(line => line.Trim()),
        ];
        Assert.Equal(
            [
                "static () => global::Hlibz.Paging.SortableDto.For<global::App.OrderDto>(),",
                "static () => global::Hlibz.Paging.SortableDto.For<global::App.CityRow>(),",
                "static () => global::Hlibz.Paging.SortableDto.For<global::App.NoDefaultDto>(),",
                "static () => global::Hlibz.Paging.SortableDto.For<global::App.InheritedDto>(),",
            ],
            listed);
    }

    [Fact]
    public void ValidateAll_OnGeneratedAssembly_NamesOnlyTheInvalidDto()
    {
        (_, Compilation output) = Compiler.Generate(Dtos);
        Assembly assembly = Compiler.Load(output);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => SortableDto.ValidateAll([assembly]));

        Assert.Equal(
            "App.NoDefaultDto must have exactly one property marked with [Sortable(IsDefault = true)], found 0.",
            exception.Message);
    }

    [Fact]
    public void ValidateAll_WithValidDtosOnly_Passes()
    {
        (_, Compilation output) = Compiler.Generate("""
            namespace App;

            public sealed class OrderDto
            {
                [Sortable(IsDefault = true)]
                public int Id { get; init; }
            }
            """);

        SortableDto.ValidateAll([Compiler.Load(output)]);
    }

    [Fact]
    public void ValidateAll_OnAssemblyWithoutGenerator_Throws()
    {
        Assembly assembly = Compiler.Load(Compiler.Compile("""
            namespace App;

            public sealed class OrderDto
            {
                [Sortable(IsDefault = true)]
                public int Id { get; init; }
            }
            """));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => SortableDto.ValidateAll([assembly]));

        Assert.Contains("source generator didn't run", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WithoutLibraryReference_WritesNothing()
    {
        (string? manifest, _) = Compiler.Generate("namespace App; public sealed class Plain { }", referenceLibrary: false);

        Assert.Null(manifest);
    }

    [Fact]
    public void ValidateAll_OnAssemblyNotReferencingPaging_HasNothingToCheck()
    {
        SortableDto.ValidateAll([typeof(object).Assembly, typeof(SortableDto).Assembly]);
    }
}
