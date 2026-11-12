# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this is

Two NuGet packages released together from one tag, extracted from Lighthouse's paging code
(`lighthouse-api/src/Common`: `Lighthouse.Common.Core/Paging`, `Lighthouse.Common.API/Models`,
`Lighthouse.Common.Infrastructure/Database/Extensions/QueryableExtensions.cs`):

- `src/Hlibz.Paging`: `PagingParams` (page from 1, default page size 100, `All`, `Skip` capped
  so a huge page can't overflow), `SortingParam`/`SortingParams`, `[Sortable]`,
  `PagedData<T>` + `PagingMetadata`, `SortingExtensions` (`OrderBy(SortingParams)` builds the
  `OrderBy`/`ThenBy` expression, strings via `ToUpper()`; in-memory `Paginate`), and
  `SortableDto`: a DTO's `[Sortable]` properties (cached per type, exactly one `IsDefault` or
  `InvalidOperationException`), `TryParse` of a client's sort string (`Internal/SortStringParser`:
  `key[:asc|desc],...`; parsed keys name properties as declared; errors name the allowed
  properties through an optional `JsonNamingPolicy`), and `ValidateAll(assemblies)`, which checks
  every closed type with a `[Sortable]` property and throws one exception naming every bad DTO.
  Keys match properties in any casing (`Internal/PropertyLookup`: `_` and `-` ignored,
  case-insensitive); no keys = sort by `Id`; a key naming no property throws
  `SortingParamNotFoundException` naming every bad key. No dependencies.
- `src/Hlibz.Paging.EntityFrameworkCore`: `PaginateAsync` (count, then the page; `All` in one
  query). EF Core is referenced per target framework (8/9/10/11, see `Directory.Packages.props`).

Query binding (`page`, `per_page`, `sort` on a base query class) deliberately isn't a package:
its parameter names, maximum page size and 400 mapping are each API's own conventions, and
Lighthouse keeps its `BaseGetQuery`, exception middleware and OpenAPI transformers. Add an
AspNetCore package only when a second API shows what is shared.

Changes Lighthouse feels: `BaseGetQuery.GetSortingParams` calls
`SortableDto.For<TDto>().TryParse(Sort, JsonNamingPolicy.SnakeCaseLower, ...)`;
`SortableQueryValidator.ValidateAll` becomes `SortableDto.ValidateAll`; `sort=name` (no
direction) is ascending; parsed keys name C# properties, not snake_case.

## Requires

* .NET 11 SDK, plus the .NET 8, 9 and 10 runtimes to run every test target.

## Commands

```bash
dotnet build
dotnet test
dotnet test -f net11.0
dotnet pack -c Release -o ./nupkg
```

## Conventions

StyleCop + Sonar + `AnalysisMode=All`, warnings as errors; every public member has XML docs.
Explicit types, no `var`. Implementation details are internal, in `Internal/`. Tests: xUnit v3 on
Microsoft Testing Platform, named `Subject_WithCondition_ExpectedOutcome`; EF tests run against
SQLite in memory.

Targets net8.0-net11.0 from one source, C# 14. Versions are central (`Directory.Packages.props`);
shared NuGet metadata and `Version` live in `Directory.Build.props`; release by pushing a
`vX.Y.Z` tag (both packages, one version). Each package has its own `src/<Package>/README.md`
(packed, shown on nuget.org, absolute links only); the root `README.md` is the full
documentation; keep them in step when an API changes.
