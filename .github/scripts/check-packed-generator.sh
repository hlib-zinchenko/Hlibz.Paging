#!/usr/bin/env bash
# Checks the packed packages the way consumers get them, which the tests can't: they reference
# the analyzer and source generator as a project analyzer. Builds, outside the repo, two projects
# that get Hlibz.Paging only indirectly:
#   Shared  a class library referencing only Hlibz.Paging.EntityFrameworkCore,
#   Api     a console app referencing only Shared,
# each with one sortable DTO, then runs Api: each assembly must carry the generated list of its
# DTOs, and SortableDto.ValidateAll must pass on both. ValidateAll throws for an assembly the
# generator didn't run on, so a generator that is missing from the package, doesn't load or
# doesn't reach an indirect consumer fails the check.
#
# Usage: check-packed-generator.sh <directory holding the .nupkg files>
set -euo pipefail

artifacts="$(cd "$1" && pwd)"
package="$(ls "$artifacts"/Hlibz.Paging.[0-9]*.nupkg)"
version="$(basename "$package" .nupkg)"
version="${version#Hlibz.Paging.}"

# The physical path: restore drops a project reference reached through a symlink (macOS /var).
work="$(cd "$(mktemp -d)" && pwd -P)"
trap 'rm -rf "$work"' EXIT
# Its own package cache: the packed version must never land in the machine-wide one.
export NUGET_PACKAGES="$work/packages"

# The packages only from the fresh pack; EF Core from nuget.org.
cat > "$work/nuget.config" <<EOF
<configuration>
  <packageSources>
    <clear />
    <add key="artifacts" value="$artifacts" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="artifacts">
      <package pattern="Hlibz.Paging" />
      <package pattern="Hlibz.Paging.*" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
EOF

mkdir "$work/Shared" "$work/Api"

cat > "$work/Shared/Shared.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Hlibz.Paging.EntityFrameworkCore" Version="$version" />
  </ItemGroup>
</Project>
EOF

cat > "$work/Shared/OrderDto.cs" <<'EOF'
using Hlibz.Paging;

namespace Shared;

public sealed class OrderDto
{
    [Sortable(IsDefault = true)]
    public int Id { get; init; }
}
EOF

cat > "$work/Api/Api.csproj" <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../Shared/Shared.csproj" />
  </ItemGroup>
</Project>
EOF

cat > "$work/Api/Program.cs" <<'EOF'
using System.Reflection;
using Hlibz.Paging;
using Shared;

Assembly[] assemblies = [typeof(CustomerDto).Assembly, typeof(OrderDto).Assembly];
SortableDto.ValidateAll(assemblies);
int[] listed = [.. assemblies.Select(assembly => assembly.GetCustomAttribute<SortableDtoManifestAttribute>()?.Dtos.Count ?? -1)];
Console.WriteLine($"Sortable DTOs listed per assembly: {string.Join(", ", listed)}");
return listed.All(count => count == 1) ? 0 : 1;

internal sealed class CustomerDto
{
    [Sortable(IsDefault = true)]
    public string Name { get; init; } = string.Empty;
}
EOF

echo "Checking Hlibz.Paging(.EntityFrameworkCore) $version from $artifacts"
dotnet run --project "$work/Api/Api.csproj"
