# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this is

Two NuGet packages released together from one tag, extracted from Lighthouse's paging code
(`lighthouse-api/src/Common`: `Lighthouse.Common.Core/Paging`, `Lighthouse.Common.API/Models`,
`Lighthouse.Common.Infrastructure/Database/Extensions/QueryableExtensions.cs`):

- `src/Hlibz.Paging`: `PagingParams` (page from 1, default page size 100, `All`, `Skip` capped
  so a huge page can't overflow), `SortingParam`/`SortingParams`, `[Sortable]`,
  `PagedData<T>` + `PagingMetadata` (every property `required` + `init`, so OpenAPI schemas
  list them as required and System.Text.Json reads them back; a parameterless constructor for
  serializers and a `[SetsRequiredMembers]` one that works out `TotalPages`/`HasNextPage`), `SortingExtensions` (`OrderBy(SortingParams)` builds the
  `OrderBy`/`ThenBy` expression, strings via `ToUpper()`; in-memory `Paginate`, which sorts the
  same plan (`Internal/SortPlan`) with delegates rather than compiling an expression), and
  `SortableDto`: a DTO's `[Sortable]` properties (cached per type, exactly one `IsDefault` or
  `InvalidOperationException`), `TryParse` of a client's sort string (`Internal/SortStringParser`:
  `key[:asc|desc],...`; parsed keys name properties as declared; errors name the allowed
  properties through an optional `JsonNamingPolicy`), and `ValidateAll(assemblies)`, which checks
  every closed, non-abstract type with a `[Sortable]` property as listed by the generated
  `SortableDtoManifestAttribute` (no scan; an assembly referencing Hlibz.Paging without one
  throws), or `ValidateAll(types)`, which
  checks the types given; either throws one exception naming every bad DTO.
  Keys match properties in any casing (`Internal/PropertyLookup`: `_` and `-` ignored,
  case-insensitive); no keys = sort by `Id`, and `Id` (when the type has one and no key names it)
  breaks ties, so pages over a non-unique key are stable; in memory (`EnumerableQuery`) a
  string key is null-safe, a database query keeps plain `UPPER(column)`; a key naming no property throws
  `SortingParamNotFoundException` naming every bad key. No dependencies.
- `src/Hlibz.Paging.Generators` (netstandard2.0, Roslyn 4.8, packed inside Hlibz.Paging under
  `analyzers/dotnet/cs` with `buildTransitive/Hlibz.Paging.targets`): `SortableDtoAnalyzer`
  reports `HPAG001` (not exactly one default, the runtime check as a build error) and
  `SortableDtoManifestGenerator` writes each assembly's DTO list, invalid ones included.
  `SortableDtoSymbols` reads `[Sortable]` the way `SortableDto` does (inherited, overrides).
  Tests and the sample reference it as an analyzer; the test DTOs that are invalid on purpose
  suppress `HPAG001`. CI's `.github/scripts/check-packed-generator.sh` builds an indirect
  consumer from the packed nupkg. Rules are tracked in `AnalyzerReleases.*.md` and the README's
  "Build errors".
- `src/Hlibz.Paging.EntityFrameworkCore`: `PaginateAsync` (count, then the page; `All` in one
  query). EF Core is referenced per target framework (10/11, see `Directory.Packages.props`).
- `samples/Orders.Api`: a plain minimal API on EF Core SQLite in memory (one connection kept
  open, seeded at startup) using both packages and no other Hlibz package: `/orders`
  (`PaginateAsync` on a projected DTO, a bad sort as `Results.ValidationProblem`),
  `/orders/export` (`PagingParams.All`), `/customers` (in-memory `Paginate`) and
  `SortableDto.ValidateAll` at startup. net11.0 only, never packed. `samples/README.md` maps each
  request to what it shows; keep it and `Orders.Api.http` in step with the API.

Query binding (`page`, `per_page`, `sort` on a base query class) deliberately isn't a package:
its parameter names, maximum page size and 400 mapping are each API's own conventions, and
Lighthouse keeps its `BaseGetQuery`, exception middleware and OpenAPI transformers. Add an
AspNetCore package only when a second API shows what is shared.

Changes Lighthouse feels: `BaseGetQuery.GetSortingParams` calls
`SortableDto.For<TDto>().TryParse(Sort, JsonNamingPolicy.SnakeCaseLower, ...)`;
`SortableQueryValidator.ValidateAll` becomes `SortableDto.ValidateAll`; `sort=name` (no
direction) is ascending; parsed keys name C# properties, not snake_case.

## Requires

* The .NET SDK `global.json` pins (11.0.100, patches roll forward; Dependabot bumps it), plus
  the .NET 10 runtime to run both test targets. Pinned because warnings are errors and
  `AnalysisMode=All`: a new SDK feature band brings new analyzer rules.

## Commands

```bash
dotnet build
dotnet test
dotnet test -f net11.0
dotnet pack -c Release -o ./nupkg
dotnet run --project samples/Orders.Api    # :5390, try Orders.Api.http
```

## Conventions

StyleCop + Sonar + `AnalysisMode=All`, warnings as errors; every public member has XML docs.
Explicit types, no `var`. Implementation details are internal, in `Internal/`. Tests: xUnit v3 on
Microsoft Testing Platform, named `Subject_WithCondition_ExpectedOutcome`; EF tests run against
SQLite in memory.

Targets net10.0 and net11.0 from one source, C# 14. Versions are central
(`Directory.Packages.props`); shared NuGet metadata and `Version` live in
`Directory.Build.props`; release by pushing a `vX.Y.Z` tag (both packages, one version; see
Releasing). Each package has its own `src/<Package>/README.md` (packed, shown on nuget.org,
absolute links only); the root `README.md` is the full documentation; keep them in step when an
API changes.

## Releasing

1. Set `Version` in `Directory.Build.props`, commit, then push the tag `vX.Y.Z`.
2. The user approves the `release` environment run.
3. Once the run succeeds and
   `https://api.nuget.org/v3-flatcontainer/<lowercase id>/index.json` lists the version, Claude
   creates the GitHub release that `PackageReleaseNotes` links to:
   `gh release create vX.Y.Z --verify-tag --title X.Y.Z [--prerelease when the version has a suffix] --notes-file <notes>`.
   Claude writes the notes from `git log <previous tag>..vX.Y.Z`: breaking changes first, then
   features and fixes, then one nuget.org link per package. Nothing is attached.
